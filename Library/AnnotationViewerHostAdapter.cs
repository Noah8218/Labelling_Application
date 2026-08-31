using OpenVisionLab.ImageCanvas.Rendering;
using System;
using System.Windows.Forms;

namespace MvcVisionSystem
{
    public sealed class AnnotationViewerHostAdapter : IDisposable
    {
        private readonly AnnotationViewer viewer;
        private bool disposed;

        public AnnotationViewerHostAdapter(AnnotationViewer viewer, Control host, bool onlyDragMode = false)
        {
            this.viewer = viewer ?? throw new ArgumentNullException(nameof(viewer));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Canvas = viewer.AttachToWinFormsHost(Host, onlyDragMode);
        }

        public Control Host { get; }

        public ImageCanvasControl Canvas { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            viewer.DetachWinFormsCanvas(Canvas);
        }
    }
}
