using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Windows.Input;

namespace WPFDrawing {
    [DataContract]
    public class SelectableColor : NamedMemberOfRefreshable, ICloneable {
        [DataMember]
        public Color SelectedColor { get; set; }

        public ICommand PickColorCommand { get; private set; }
        private void PickColor() {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK) {
                SelectedColor = colorDialog.Color;
                RefreshableContainer?.Refresh();
            }
        }

        #region creation
        private SelectableColor() {
            BaseName = "Color Picker"; // ?
            PickColorCommand = new RelayCommand(PickColor);
        }

        public SelectableColor(IRefreshableContainer refreshableContainer, Color initialColor) : this() {
            RefreshableContainer = refreshableContainer;
            SelectedColor = initialColor;
        }

        public SelectableColor(IRefreshableContainer refreshableContainer, string initalColorName)
            : this(refreshableContainer, Color.FromName(initalColorName)) { }

        public SelectableColor(string initalColorName)
            : this(null, initalColorName) { }

        public object Clone()
            => new SelectableColor(RefreshableContainer, SelectedColor);
        #endregion
    }
}
