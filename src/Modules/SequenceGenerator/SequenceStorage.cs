using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.SequenceGenerator{
    [PublicAPI]
    public interface ISequenceStorage{
        string Name{ get; set; }
        string CustomSequence{ get; set; }
        long NextSequence{ get; set; }
        string ReleasedSequences{ get; set; }
        string SequenceMember{ get; set; }
    }

    [NonPersistent]
    public class ObjectType{
        public string Name{ get; set; }
        [Browsable(false)]
        public Type Type{ get; set; }
    }

    [NonPersistent][XafDefaultProperty(nameof(Caption))]
    public class ObjectMember{
        public  string Caption{ get; set; }
        [Browsable(false)]
        public string Name{ get; set; }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SequenceStorageKeyNameAttribute:Attribute{
        public Type Type{ get; }

        public SequenceStorageKeyNameAttribute(Type type){
            Type = type;
        }

        public static Type FindConsumer(Type sequenceType){
            return XafTypesInfo.Instance.PersistentTypes.SelectMany(info => info
                .FindAttributes<SequenceStorageKeyNameAttribute>()
                .Where(attribute => attribute.Type == sequenceType).Select(attribute => info.Type)).FirstOrDefault()??sequenceType;
        }

    }
    [DeferredDeletion(false)][DefaultProperty(nameof(Name))]
    public class SequenceStorage : XPBaseObject, ISequenceStorage,IObjectSpaceLink{
        private long _nextSequence;
        public SequenceStorage(Session session)
            : base(session){
        }
        
        string _name;
        
        [Size(255)][Key]
        [Browsable(false)]
        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        string _customSequence;

        protected override void OnLoaded(){
            base.OnLoaded();
            if (!(Session is ExplicitUnitOfWork)){
                var sequenceStorageName = Name;
                if (sequenceStorageName != null){
                    var modelClass =CaptionHelper.ApplicationModel.BOModel.GetClass(SequenceStorageKeyNameAttribute.FindConsumer(XafTypesInfo.Instance.FindTypeInfo(sequenceStorageName).Type));
                    Type = new ObjectType(){Name = modelClass.Caption, Type = modelClass.TypeInfo.Type};
                    Member=new ObjectMember(){Caption = modelClass.GetMemberCaption(SequenceMember),Name = SequenceMember};
                    var customSequence = CustomSequence;
                    if (customSequence!=null){
                        modelClass =CaptionHelper.ApplicationModel.BOModel.GetClass(XafTypesInfo.Instance.FindTypeInfo(customSequence).Type);
                        CustomType=new ObjectType(){Name = modelClass.Caption,Type = modelClass.TypeInfo.Type};
                    }
                }   
            }
        }

        [Size(255)][Browsable(false)]
        public string CustomSequence{
            get => _customSequence;
            set => SetPropertyValue(nameof(CustomSequence), ref _customSequence, value);
        }

        ObjectType _type;
        [DataSourceProperty(nameof(Types))][NonPersistent][RuleRequiredField][ImmediatePostData]
        public ObjectType Type{
            get => _type;
            set => SetPropertyValue(nameof(Type), ref _type, value);
        }

        ObjectType _customType;
        [DataSourceProperty(nameof(CustomTypes))][NonPersistent]
        public ObjectType CustomType{
            get => _customType;
            set => SetPropertyValue(nameof(CustomType), ref _customType, value);
        }

        
        [Browsable(false)]
        public List<ObjectType> Types=>CaptionHelper.ApplicationModel.BOModel
            .Where(_ => typeof(IXPObject).IsAssignableFrom(_.TypeInfo.Type)&&_.TypeInfo.IsPersistent)
            .Select(_ => new ObjectType(){Name = _.Caption,Type=_.TypeInfo.Type})
            .ToList();

        [Browsable(false)]
        public List<ObjectType> CustomTypes=>CaptionHelper.ApplicationModel.BOModel
            .Where(_ =>Type!=null&&_.TypeInfo.Type!=Type.Type&& _.TypeInfo.Type.IsAssignableFrom(Type.Type))
            .Where(_ => typeof(IXPObject).IsAssignableFrom(_.TypeInfo.Type))
            .Select(_ => new ObjectType(){Name = _.Caption,Type=_.TypeInfo.Type}).ToList();

        public override string ToString(){
            return $"{Name}";
        }

        [Browsable(false)]
        public List<ObjectMember> Members => Type == null ? new List<ObjectMember>()
            : Type.Type.ToTypeInfo().Members.Where(info =>info.IsPublic&& info.MemberType == typeof(long)).Select(info =>
                new ObjectMember() {
                    Caption = CaptionHelper.ApplicationModel.BOModel.GetClass(info.Owner.Type).GetMemberCaption(info.Name),
                    Name=info.Name
                }).ToList();
        
        ObjectMember _objectMember;
        [RuleRequiredField][NonPersistent][DataSourceProperty(nameof(Members))]
        public ObjectMember Member{
            get => _objectMember;
            set => SetPropertyValue(nameof(Member), ref _objectMember, value);
        }
        string _sequenceMember;
        [RuleRequiredField][Browsable(false)]
        public string SequenceMember{
            get => _sequenceMember;
            set => SetPropertyValue(nameof(SequenceMember), ref _sequenceMember, value);
        }

        public long NextSequence{
            get => _nextSequence;
            set => SetPropertyValue(nameof(NextSequence), ref _nextSequence, value);
        }

        string _releasedSequences;
        

        [VisibleInDetailView(false)]
        [Size(-1)]
        public string ReleasedSequences{
            get => _releasedSequences;
            set => SetPropertyValue(nameof(ReleasedSequences), ref _releasedSequences, value);
        }

        [Browsable(false)]
        public IObjectSpace ObjectSpace{ get; set; }
    }

}