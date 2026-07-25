module;
#include <algorithm>
#include <atomic>
#include <condition_variable>
#include <cstddef>
#include <functional>
#include <future>
#include <latch>
#include <memory>
#include <mutex>
#include <queue>
#include <thread>
#include <type_traits>
#include <utility>
#include <vector>
export module utility.io.concurrent;

export class ThreadPool {
public:
    explicit ThreadPool(size_t n = defaultSize()) {
        workers_.reserve(n);
        for (size_t i = 0; i < n; ++i) {
            workers_.emplace_back([this](std::stop_token stoken) {
                for (;;) {
                    std::function<void()> task;
                    {
                        std::unique_lock lk(mu_);
                        cv_.wait(lk, stoken, [this] { return stopping_ || !q_.empty(); });
                        if ((stoken.stop_requested() || stopping_) && q_.empty()) return;
                        task = std::move(q_.front());
                        q_.pop();
                    }
                    task();
                }
            });
        }
    }

    ~ThreadPool() {
        {
            std::lock_guard lk(mu_);
            stopping_ = true;
        }
        for (auto& w : workers_) w.request_stop();
        cv_.notify_all();
    }

    template<class F, class... A>
    auto submit(F&& f, A&&... a) -> std::future<std::invoke_result_t<F, A...>> {
        using R = std::invoke_result_t<F, A...>;
        auto task = std::make_shared<std::packaged_task<R()>>(
            [fn = std::forward<F>(f), ...xs = std::forward<A>(a)]() mutable -> R {
                if constexpr (std::is_void_v<R>) fn(std::move(xs)...);
                else return fn(std::move(xs)...);
            });
        auto fut = task->get_future();
        {
            std::lock_guard lk(mu_);
            q_.emplace([task] { (*task)(); });
        }
        cv_.notify_one();
        return fut;
    }

    void submitVoid(std::function<void()> f) {
        {
            std::lock_guard lk(mu_);
            q_.emplace(std::move(f));
        }
        cv_.notify_one();
    }

    [[nodiscard]] size_t threadCount() const noexcept { return workers_.size(); }
    [[nodiscard]] size_t size() const noexcept { return workers_.size(); }
    [[nodiscard]] static size_t defaultSize() noexcept {
        auto n = std::thread::hardware_concurrency();
        return std::clamp<size_t>(n ? n * 2 : 4, 4, 16);
    }

    ThreadPool(const ThreadPool&) = delete;
    ThreadPool& operator=(const ThreadPool&) = delete;

private:
    std::vector<std::jthread> workers_;
    std::queue<std::function<void()>> q_;
    std::mutex mu_;
    std::condition_variable_any cv_;
    bool stopping_ = false;
};

export class Latch {
public:
    explicit Latch(int n) : latch_(static_cast<std::ptrdiff_t>(n)) {}
    void countDown() { latch_.count_down(); }
    void wait() { latch_.wait(); }
private:
    std::latch latch_;
};

export inline ThreadPool& getGlobalPool() {
    static ThreadPool pool;
    return pool;
}

export inline void parallelFor(size_t count, const std::function<void(size_t)>& fn, ThreadPool& pool = getGlobalPool()) {
    if (!count) return;
    if (count == 1) { fn(0); return; }
    Latch latch(static_cast<int>(count));
    for (size_t i = 0; i < count; ++i) {
        pool.submitVoid([i, &fn, &latch] { fn(i); latch.countDown(); });
    }
    latch.wait();
}

export inline void parallelForBatched(size_t count, size_t batchSize, const std::function<void(size_t)>& fn, ThreadPool& pool = getGlobalPool()) {
    if (!count) return;
    batchSize = std::max<size_t>(1, batchSize);
    const size_t batches = (count + batchSize - 1) / batchSize;
    Latch latch(static_cast<int>(batches));
    for (size_t b = 0; b < batches; ++b) {
        const size_t begin = b * batchSize;
        const size_t end = std::min(begin + batchSize, count);
        pool.submitVoid([begin, end, &fn, &latch] {
            for (size_t i = begin; i < end; ++i) fn(i);
            latch.countDown();
        });
    }
    latch.wait();
}
