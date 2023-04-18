using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    [KnownType(typeof(VertexPoint))]
    [KnownType(typeof(VertexPointController))]
    public abstract class BasePoint : Shape {
        #region stuff
        [DataMember]
        public BaseBoundedCoord CenterCoord { get; set; }

        public override string VerboseName => $"{BaseName}??";
        #endregion

        #region creation
        public BasePoint(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, SelectableColor defaultColor, string name)
            : base(refreshableContainer, shapeSerializer, name) {
            Color = new SelectableColor(this, defaultColor.SelectedColor);
        }
        #endregion
    }
}
