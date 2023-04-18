using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WPFDrawing {
    [DataContract]
    public class NamedBoundedValueController : BaseNamedBoundedValue, IRefreshableContainer {
        [DataMember]
        public HashSet<NamedBoundedValue> ControlledValues { get; private set; } = new HashSet<NamedBoundedValue>();

        public override double Value {
            get => ControlledValues.Average(val => val.Value);
            set {
                if (ControlledValues?.Count > 0) {
                    double delta = value - Value;
                    foreach (NamedBoundedValue val in ControlledValues) {
                        val.Value += delta;
                    }

                    OnPropertyChanged(nameof(Value));
                    Refresh();
                }
            }
        }

        public void Refresh(bool forceRefresh = false) {
            RefreshableContainer.Refresh(forceRefresh);
        }

        #region creation
        private NamedBoundedValueController(IRefreshableContainer refreshableContainer, string baseName) {
            RefreshableContainer = refreshableContainer;
            BaseName = baseName;
        }

        public NamedBoundedValueController(IRefreshableContainer refreshableContainer, string baseName, HashSet<NamedBoundedValue> controlledValues)
            : this(refreshableContainer, baseName) {
            if (controlledValues.Count > 0) {
                NamedBoundedValue firstItem = controlledValues.First();
                LowerBound = firstItem.LowerBound;
                UpperBound = firstItem.UpperBound;
            }

            ControlledValues = new HashSet<NamedBoundedValue>(controlledValues); // new set same refs? aa
        }

        public NamedBoundedValueController(IRefreshableContainer refreshableContainer, string baseName, params NamedBoundedValue[] controlledValues)
            : this(refreshableContainer, baseName, new HashSet<NamedBoundedValue>(controlledValues)) { }

        public NamedBoundedValueController(NamedBoundedValueController namedBoundedValueController)
            : this(namedBoundedValueController.RefreshableContainer,
                  namedBoundedValueController.BaseName,
                  namedBoundedValueController.ControlledValues) { }

        public override object Clone()
            => new NamedBoundedValueController(this);
        #endregion
    }
}
