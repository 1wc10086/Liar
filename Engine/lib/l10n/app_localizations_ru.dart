// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Russian (`ru`).
class AppLocalizationsRu extends AppLocalizations {
  AppLocalizationsRu([String locale = 'ru']) : super(locale);

  @override
  String get appTitle => 'Liar Hub';

  @override
  String get navConsole => 'Консоль';

  @override
  String get navSettings => 'Настройки';

  @override
  String get rename => 'Переименовать';

  @override
  String get cancel => 'Отмена';

  @override
  String get copy => 'Копировать';

  @override
  String get remove => 'Удалить';

  @override
  String get save => 'Сохранить';

  @override
  String get clear => 'Очистить';

  @override
  String get notConfigured => 'Не настроено, нажмите для редактирования';

  @override
  String get noFunctions => 'Нет доступных функций';

  @override
  String get configureFirst => 'Сначала завершите настройку';

  @override
  String get pathHint => 'Путь';

  @override
  String get optionHint => 'Параметр';

  @override
  String get skipOption => ' (Пропустить) ';

  @override
  String get processing => 'Обработка...';

  @override
  String get sectionShell => 'Shell';

  @override
  String get sectionAppearance => 'Внешний вид';

  @override
  String get sectionAbout => 'О приложении';

  @override
  String get sectionPermissions => 'Разрешения';

  @override
  String get sectionUpdate => 'Обновление';

  @override
  String get kernelPath => 'Путь к файлу ядра';

  @override
  String get scriptDir => 'Каталог скриптов';

  @override
  String get themeTitle => 'Тема';

  @override
  String get themeMode => 'Режим темы';

  @override
  String get themeColor => 'Цвет темы';

  @override
  String get dynamicColor => 'Динамический цвет Monet';

  @override
  String get lightMode => 'Светлый режим';

  @override
  String get darkMode => 'Тёмный режим';

  @override
  String get systemMode => 'Системная';

  @override
  String get seedDeepPurple => 'Тёмно-фиолетовый';

  @override
  String get seedIndigo => 'Индиго';

  @override
  String get seedBlue => 'Синий';

  @override
  String get seedTeal => 'Бирюзовый';

  @override
  String get seedGreen => 'Зелёный';

  @override
  String get seedOrange => 'Оранжевый';

  @override
  String get seedPink => 'Розовый';

  @override
  String get seedRed => 'Красный';

  @override
  String get seedCustom => 'Пользовательский';

  @override
  String get amoledMode => 'AMOLED режим';

  @override
  String get amoledModeDesc => 'Чёрный фон для экономии заряда';

  @override
  String get fontSetting => 'Шрифт';

  @override
  String get fontSettingDesc =>
      'Нажмите, чтобы задать путь к шрифту (пусто — системный)';

  @override
  String get fontDialogTitle => 'Указать путь к шрифту';

  @override
  String get fontPathHint => 'Введите путь к шрифту (пусто — системный)';

  @override
  String get fontPathDefault => 'Системный';

  @override
  String get textSize => 'Размер текста';

  @override
  String get textSizeDesc => 'Настройка масштаба текста';

  @override
  String get layoutSettings => 'Раскладка';

  @override
  String get switchStyleTitle => 'Стиль переключателя';

  @override
  String get switchStylePixel => 'Pixel';

  @override
  String get switchStyleMaterial => 'Material You';

  @override
  String get switchStyleToggleDesc => 'Вкл: стиль Pixel. Выкл: Material You.';

  @override
  String get aboutApp => 'О приложении';

  @override
  String get version => 'Версия 1.0.7';

  @override
  String get language => 'Язык';

  @override
  String get languageDialogTitle => 'Выбрать язык';

  @override
  String get langFollowSystem => 'Системный';

  @override
  String get langZh => 'Упрощённый китайский';

  @override
  String get langEn => 'Английский';

  @override
  String get langJa => 'Японский';

  @override
  String get langKo => 'Корейский';

  @override
  String get langVi => 'Вьетнамский';

  @override
  String get langRu => 'Русский';

  @override
  String get executorImpl => 'Реализация исполнителя';

  @override
  String get executorDialogTitle => 'Выбрать исполнителя';

  @override
  String editDialogTitle(String label) {
    return 'Изменить $label';
  }

  @override
  String get inputPathHint => 'Введите путь...';

  @override
  String cannotOpenLink(String url) {
    return 'Не удалось открыть ссылку: $url';
  }

  @override
  String get aboutTitle => 'О приложении';

  @override
  String get aboutTagline =>
      'Бесплатный инструмент с открытым кодом навсегда — доступ каждому!';

  @override
  String get sectionProject => 'Проект';

  @override
  String get sectionDevelopers => 'Разработчики';

  @override
  String get sectionCredits => 'Благодарности';

  @override
  String get githubSource => 'Исходный код на GitHub';

  @override
  String get githubSourceDesc => 'Смотреть, форкать или участвовать в проекте';

  @override
  String get issueTracker => 'Проблемы и отзывы';

  @override
  String get issueTrackerDesc => 'Сообщить об ошибке или предложить функцию';

  @override
  String get mainDeveloper => 'Главный разработчик';

  @override
  String get creditDesc1 =>
      'Реализация на C# — ключевой ориентир для реализации на C++ проекта';

  @override
  String get creditDesc2 =>
      'Реализация на C++ — источник ключевых идей для проекта';

  @override
  String get license => 'Лицензия: AGPL-3.0';

  @override
  String get copyright => '© 2026 Liar ToolKit Studio';

  @override
  String get loadingExecutors => 'Загрузка...';

  @override
  String get noExecutors => 'Исполнители не найдены';

  @override
  String get batchModeOn => 'Пакет';

  @override
  String get batchModeOff => 'Один файл';

  @override
  String get labTitle => 'Lab';

  @override
  String get pluginDrawerTitle => 'Плагины';

  @override
  String get search => 'Поиск';

  @override
  String get labEmptyHint => 'Выберите функцию плагина из бокового меню';

  @override
  String get startProcess => 'Начать';

  @override
  String get paramPathDesc => 'Выберите или введите параметр пути.';

  @override
  String get paramStringDesc => 'Введите текстовый параметр.';

  @override
  String get paramIntegerDesc => 'Введите целочисленный параметр.';

  @override
  String get paramBooleanDesc => 'Переключите логический параметр.';

  @override
  String get paramListDesc => 'Выберите одно или несколько значений из списка.';

  @override
  String get paramMapDesc =>
      'Выберите одно или несколько сопоставленных значений.';

  @override
  String get shellCardTitle => 'Shell';

  @override
  String get shellCardSubtitle => 'Легко запускайте задачи и команды.';

  @override
  String get labCardSubtitle => 'Пусть интерфейс начнёт чувствовать.';

  @override
  String get storagePermission => 'Хранилище';

  @override
  String get storagePermissionDesc =>
      'Нажмите, чтобы открыть экран разрешений Хранилища';

  @override
  String get checkUpdateOnStartup => 'Проверять обновления при запуске';

  @override
  String get checkUpdateOnStartupDesc =>
      'Автоматически проверять обновления при запуске';

  @override
  String get repoMirror => 'Зеркало репозитория';

  @override
  String get repoMirrorDesc =>
      'Использовать прокси-зеркало GitHub для загрузок';

  @override
  String get repoMirrorNone => 'Напрямую (без зеркала)';

  @override
  String get updatePageTitle => 'Обновление';

  @override
  String newVersionFound(String version) {
    return 'Доступна новая версия $version';
  }

  @override
  String get prereleaseSuffix => ' (предрелиз)';

  @override
  String publishDate(String date) {
    return 'Опубликовано: $date';
  }

  @override
  String get remindLater => 'Напомнить позже';

  @override
  String get viewDetails => 'Подробнее';

  @override
  String get updateNow => 'Обновить сейчас';

  @override
  String get connecting => 'Подключение…';

  @override
  String get downloadingUpdate => 'Загрузка обновления…';

  @override
  String get extracting => 'Распаковка…';

  @override
  String get installingFiles => 'Установка файлов…';

  @override
  String updateFailed(String error) {
    return 'Ошибка обновления: $error';
  }

  @override
  String get updateComplete => 'Обновление завершено';

  @override
  String get updateCompleteMessage =>
      'Файлы заменены. Нажмите для установки нового APK.';

  @override
  String get installNow => 'Установить сейчас';

  @override
  String cannotOpenApk(String message) {
    return 'Не удалось открыть установщик APK: $message';
  }
}
