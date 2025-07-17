using System;
using DevExpress.Persistent.Base;

namespace Xpand.Extensions.XAF.Attributes.HideInUI;
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ShowOnlyInDetailViewAttribute(bool includeCustomizationForm = true, bool includeEditor = true, bool includeModelMember = true)
    : HideInUIAttribute(DevExpress.Persistent.Base.HideInUI.All & ~(
                            DevExpress.Persistent.Base.HideInUI.DetailView | 
                            (includeCustomizationForm ? DevExpress.Persistent.Base.HideInUI.DetailViewCustomizationForm : 0) | 
                            (includeEditor ? DevExpress.Persistent.Base.HideInUI.DetailViewEditor : 0) | 
                            (includeModelMember ? DevExpress.Persistent.Base.HideInUI.ModelMember : 0)
                        ));