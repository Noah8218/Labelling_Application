using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MvcVisionSystem._2._Common
{
    public static class InspectionParameters
    {
        public class SpecAreaParameters : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropretyChanged(string propertyName)
            {
                var handler = this.PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            [DisplayName("No")]
            public int Index { get; set; } = 0;

            private Rectangle m_SpecArea = new Rectangle();

            [Browsable(false)]
            public Rectangle SpecArea
            {
                get { return m_SpecArea; }
                set
                {
                    m_SpecArea = value;
                    Bound_X = m_SpecArea.X;
                    Bound_Y = m_SpecArea.Y;
                    Bound_W = m_SpecArea.Width;
                    Bound_H = m_SpecArea.Height;
                    this.OnPropretyChanged("SpecArea");
                }
            }

            [DisplayName("X")]
            public int Bound_X { get; set; } = 0;
            [DisplayName("Y")]
            public int Bound_Y { get; set; } = 0;
            [DisplayName("W")]
            public int Bound_W { get; set; } = 0;

            [DisplayName("H")]
            public int Bound_H { get; set; } = 0;
            [Browsable(false)]
            public Rectangle PreRoi { get; set; } = new Rectangle();
            #region Offset
            [Browsable(false)]
            public float PidutialX { get; set; } = 0;
            [Browsable(false)]
            public float PidutialY { get; set; } = 0;
            #endregion
            // 상대좌표 적용이 되었는지 확인하는 파라미터
            [Browsable(false)]
            public bool Relative_Coordinates { get; set; } = false;
            public SpecAreaParameters() { }

            public SpecAreaParameters DeepCopy()
            {
                SpecAreaParameters temp = (SpecAreaParameters)this.MemberwiseClone();
                return temp;
            }
        }

        public class ConnectorParameters : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropretyChanged(string propertyName)
            {
                var handler = this.PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            #region Offset
            [Browsable(false)]
            public float PidutialX { get; set; } = 0;
            [Browsable(false)]
            public float PidutialY { get; set; } = 0;
            #endregion
            // 상대좌표 적용이 되었는지 확인하는 파라미터
            [Browsable(false)]
            public bool Relative_Coordinates { get; set; } = false;
            public ConnectorParameters() { }

            public ConnectorParameters DeepCopy()
            {
                ConnectorParameters temp = (ConnectorParameters)this.MemberwiseClone();
                return temp;
            }
        }

        public class SpecDistanceParameters : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropretyChanged(string propertyName)
            {
                var handler = this.PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            [DisplayName("No")]
            public int Index { get; set; } = 0;

            public System.Drawing.PointF FitCentersTop { get; set; } = new System.Drawing.PointF();
            public System.Drawing.PointF FitCentersBtm { get; set; } = new System.Drawing.PointF();

            #region Offset
            [Browsable(false)]
            public float PidutialX_Top { get; set; } = 0;
            [Browsable(false)]
            public float PidutialY_Top { get; set; } = 0;

            [Browsable(false)]
            public float PidutialX_Btm { get; set; } = 0;
            [Browsable(false)]
            public float PidutialY_Btm { get; set; } = 0;
            #endregion
            // 상대좌표 적용이 되었는지 확인하는 파라미터
            [Browsable(false)]
            public bool Relative_Coordinates { get; set; } = false;
            public SpecDistanceParameters() { }

            public SpecDistanceParameters DeepCopy()
            {
                SpecDistanceParameters temp = (SpecDistanceParameters)this.MemberwiseClone();
                return temp;
            }
        }


        public class InspectionRectangleParameters : INotifyPropertyChanged
        {
            // 추후 해당 파라미터로 프로젝트를 수행하게 된다면 해당 부분을 true로 변경

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropretyChanged(string propertyName)
            {
                var handler = this.PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            [DisplayName("No")]
            public int Index { get; set; } = 0;

            [Browsable(false)]
            public string Name { get; set; } = "";
            [Browsable(false)]

            private Rectangle m_Roi = new Rectangle();

            [Browsable(false)]
            public Rectangle Roi
            {
                get { return m_Roi; }
                set
                {
                    m_Roi = value;
                    Bound_X = m_Roi.X;
                    Bound_Y = m_Roi.Y;
                    Bound_W = m_Roi.Width;
                    Bound_H = m_Roi.Height;
                    this.OnPropretyChanged("Roi");
                }
            }

            [DisplayName("X")]
            public int Bound_X { get; set; } = 0;
            [DisplayName("Y")]
            public int Bound_Y { get; set; } = 0;
            [DisplayName("W")]
            public int Bound_W { get; set; } = 0;

            [DisplayName("H")]
            public int Bound_H { get; set; } = 0;

            [Browsable(false)]
            public Rectangle PreRoi { get; set; } = new Rectangle();

            [DisplayName("알고리즘")]
            [Browsable(true)]
            public DEFINE.ALGORITHM Algorithm { get; set; } = DEFINE.ALGORITHM.MATCING;
            #region Offset
            [Browsable(false)]
            public float PidutialX { get; set; } = 0;
            [Browsable(false)]
            public float PidutialY { get; set; } = 0;
            #endregion
            #region Blob

            // 이미지 반전 유무
            [DisplayName("반전유무")]
            [Browsable(true)]
            public bool USE_BITWISENOT { get; set; } = true;

            // 스레숄드 값
            [DisplayName("임계값")]
            [Browsable(true)]
            public int THRESHOLD { get; set; } = 30;

            // blob 검출 최저 사이즈
            [DisplayName("최소크기")]
            [Browsable(true)]
            public int MIN_AREA { get; set; } = 50;

            // blob 검출 최대 사이즈
            [DisplayName("최대크기")]
            [Browsable(true)]
            public int MAX_AREA { get; set; } = 500000;

            // blob 검출 카운터
            [DisplayName("검출개수")]
            [Browsable(true)]
            public int InspCount { get; set; } = 1;
            #endregion
            #region Matching

            // 패턴 검출 스코어
            [Browsable(true)]
            public double SCORE { get; set; } = 0.7;

            // 해당 roi를 검사에 사용할것인지 확인하는 파라미터
            [DisplayName("ROI 사용/미사용")]
            [Browsable(true)]
            public bool USE_ROI { get; set; } = true;

            // 패턴 이미지 위치
            [DisplayName("이미지 위치")]
            [Browsable(true)]
            public string PATTERN_PATH { get; set; } = "TEMP";

            // 상대좌표 적용이 되었는지 확인하는 파라미터
            [Browsable(false)]
            public bool Relative_Coordinates { get; set; } = false;
            #endregion

            public InspectionRectangleParameters() { }

            public InspectionRectangleParameters DeepCopy()
            {
                InspectionRectangleParameters temp = (InspectionRectangleParameters)this.MemberwiseClone();
                return temp;
            }

            public InspectionRectangleParameters(InspectionRectangleParameters source)
            {
                this.Roi = source.Roi;
                this.Algorithm = source.Algorithm;
                this.Name = source.Name;
                this.Index = source.Index;
                this.PidutialX = source.PidutialX;
                this.PidutialY = source.PidutialY;
                this.USE_BITWISENOT = source.USE_BITWISENOT;
                this.THRESHOLD = source.THRESHOLD;
                this.MIN_AREA = source.MIN_AREA;
                this.MAX_AREA = source.MAX_AREA;
                this.PATTERN_PATH = source.PATTERN_PATH;
                this.SCORE = source.SCORE;
                this.USE_ROI = source.USE_ROI;
                this.InspCount = source.InspCount;
            }
        }
    }
}
