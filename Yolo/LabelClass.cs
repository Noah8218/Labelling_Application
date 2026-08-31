using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;

namespace MvcVisionSystem.Yolo
{
    // Preserve the serialized item type name used by existing VISION.xml files.
    [XmlType(TypeName = "CClassItem")]
    public class LabelClass
    {
        public LabelClass() { }

        private Color m_DrawColor = Color.FromArgb(0, 0, 0);

        public string Text { get; set; } = "";

        // Archived classes remain in the serialized canonical list so existing
        // YOLO numeric labels keep their original meaning.
        public bool IsArchived { get; set; }

        [Browsable(false)]
        public string ClrGridHtml
        {
            get { return ColorTranslator.ToHtml(m_DrawColor); }
            set { DrawColor = ColorTranslator.FromHtml(value); }
        }

        [XmlIgnore]
        public System.Drawing.Color DrawColor
        {
            get { return m_DrawColor; }
            set { m_DrawColor = value; }
        }
    }
}
