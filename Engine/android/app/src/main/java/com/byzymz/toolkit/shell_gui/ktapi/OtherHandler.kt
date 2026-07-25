package com.byzymz.toolkit.shell_gui.ktapi

import dev.mccue.jogg.Packet
import dev.mccue.jogg.Page
import dev.mccue.jogg.StreamState
import dev.mccue.jogg.SyncState
import dev.mccue.jorbis.Comment
import dev.mccue.jorbis.Info
import java.io.File
import java.io.FileInputStream
import java.io.FileOutputStream

object OtherHandler {
    @JvmStatic
    fun revorb(input: String, output: String): Boolean = runCatching {
        val source = File(input)
        val target = File(output)
        require(source.isFile)
        target.parentFile?.mkdirs()
        if (target.exists()) require(target.delete())

        FileInputStream(source).use { inputStream ->
            FileOutputStream(target).use { outputStream ->
                repair(inputStream, outputStream)
            }
        }
        target.isFile && target.length() > 0L
    }.getOrDefault(false)

    private fun repair(input: FileInputStream, output: FileOutputStream) {
        val sync = SyncState().also { it.init() }
        val inputStream = StreamState()
        val outputStream = StreamState()
        val page = Page()
        val packet = Packet()
        val info = Info().also { it.init() }
        val comment = Comment().also { it.init() }

        try {
            require(readPage(input, sync, page))
            inputStream.init(page.serialno())
            outputStream.init(page.serialno())
            require(inputStream.pagein(page) == 0)
            require(inputStream.packetout(packet) == 1)
            require(info.synthesis_headerin(comment, packet) == 0)
            outputStream.packetin(packet)

            var headers = 0
            while (headers < 2) {
                if (sync.pageout(page) != 1) {
                    require(readPage(input, sync, page))
                    continue
                }
                require(inputStream.pagein(page) == 0)
                while (headers < 2 && inputStream.packetout(packet) == 1) {
                    require(info.synthesis_headerin(comment, packet) == 0)
                    outputStream.packetin(packet)
                    headers++
                }
            }
            while (outputStream.flush(page) != 0) writePage(output, page)

            var granule = 0L
            var previousBlock = 0
            var lastPacket: Packet? = null
            var eos = false

            while (!eos) {
                when (sync.pageout(page)) {
                    0 -> {
                        if (!readMore(input, sync)) break
                    }
                    -1 -> throw IllegalArgumentException("Corrupted Ogg page")
                    else -> {
                        if (page.eos() != 0) eos = true
                        require(inputStream.pagein(page) == 0)
                        while (inputStream.packetout(packet) != 0) {
                            val block = info.blocksize(packet)
                            require(block > 0)
                            if (previousBlock != 0) granule += (previousBlock + block).toLong() / 4L
                            previousBlock = block
                            packet.granulepos = granule
                            packet.packetno = lastPacket?.packetno?.plus(1L) ?: 0L
                            if (packet.e_o_s == 0) {
                                outputStream.packetin(packet)
                                while (outputStream.pageout(page) != 0) writePage(output, page)
                            }
                            lastPacket = copyPacket(packet)
                        }
                    }
                }
            }

            val finalPacket = requireNotNull(lastPacket)
            finalPacket.e_o_s = 1
            finalPacket.granulepos = granule
            outputStream.packetin(finalPacket)
            while (outputStream.flush(page) != 0) writePage(output, page)
        } finally {
            info.clear()
            inputStream.clear()
            outputStream.clear()
            sync.clear()
        }
    }

    private fun readPage(input: FileInputStream, sync: SyncState, page: Page): Boolean {
        while (sync.pageout(page) != 1) if (!readMore(input, sync)) return false
        return true
    }

    private fun readMore(input: FileInputStream, sync: SyncState): Boolean {
        val offset = sync.buffer(8192)
        val count = input.read(sync.data, offset, 8192)
        if (count <= 0) return false
        sync.wrote(count)
        return true
    }

    private fun writePage(output: FileOutputStream, page: Page) {
        output.write(page.header_base, page.header, page.header_len)
        output.write(page.body_base, page.body, page.body_len)
    }

    private fun copyPacket(source: Packet) = Packet().apply {
        packet_base = source.packet_base.copyOfRange(source.packet, source.packet + source.bytes)
        packet = 0
        bytes = source.bytes
        b_o_s = source.b_o_s
        e_o_s = source.e_o_s
        granulepos = source.granulepos
        packetno = source.packetno
    }
}
