namespace MvcVisionSystem
{
    public enum WpfAnnotationShortcutKind
    {
        None,
        SelectTool,
        SelectClass,
        OpenClassCatalog,
        RepeatLast,
        DuplicateSelected,
        ToggleShortcutHelp
    }

    public sealed class WpfAnnotationShortcut
    {
        public static WpfAnnotationShortcut None { get; } = new WpfAnnotationShortcut(
            WpfAnnotationShortcutKind.None);

        public WpfAnnotationShortcut(
            WpfAnnotationShortcutKind kind,
            WpfAnnotationTool tool = WpfAnnotationTool.Select,
            int classIndex = -1)
        {
            Kind = kind;
            Tool = tool;
            ClassIndex = classIndex;
        }

        public WpfAnnotationShortcutKind Kind { get; }

        public WpfAnnotationTool Tool { get; }

        public int ClassIndex { get; }
    }
}
