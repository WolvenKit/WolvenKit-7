using System.Collections.Generic;
using System.IO;

using System.Diagnostics;
using System;
using System.Linq;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using FastMember;

namespace WolvenKit.CR2W.Types
{
    [REDMeta(EREDMetaInfo.REDStruct)]
    public class SBlockData : CVariable
    {

        [Ordinal(0)] [RED] public CMatrix3x3 rotationMatrix { get; set; }
        [Ordinal(1)] [RED] public SVector3D position { get; set; }
        [Ordinal(2)] [RED] public CUInt16 streamingRadius { get; set; }
        [Ordinal(3)] [RED] public CUInt16 flags { get; set; }
        [Ordinal(4)] [RED] public CUInt32 occlusionSystemID { get; set; }

        public Enums.BlockDataObjectType packedObjectType;

        public CVariable SBlockDataPackedObject;

        public SBlockData(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name)
        {
        }

        public void CreatePackedObject()
        {
            switch (packedObjectType)
            {
                //TODO: Read the different objects
                case Enums.BlockDataObjectType.Collision:
                {
                    SBlockDataPackedObject = new SBlockDataCollisionObject(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }

                case Enums.BlockDataObjectType.Particles:
                {
                    SBlockDataPackedObject = new SBlockDataParticles(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
                case Enums.BlockDataObjectType.RigidBody:
                {
                    SBlockDataPackedObject = new SBlockDataRigidBody(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
                case Enums.BlockDataObjectType.Mesh:
                {
                    // MFP - we need this for the scene viewer
                    SBlockDataPackedObject = new SBlockDataMeshObject(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
                case Enums.BlockDataObjectType.Dimmer:
                {
                    SBlockDataPackedObject = new SBlockDataDimmer(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }

                case Enums.BlockDataObjectType.PointLight:
                {
                    SBlockDataPackedObject = new SBlockDataLight(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
                case Enums.BlockDataObjectType.SpotLight:
                {
                    SBlockDataPackedObject = new SBlockDataSpotLight(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
                case Enums.BlockDataObjectType.Decal:
                {
                    SBlockDataPackedObject = new SBlockDataDecal(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
                case Enums.BlockDataObjectType.Cloth: //TODO: Implement CClothComponent here
                case Enums.BlockDataObjectType.Destruction: //TODO: Implement CDestructionComponent here
                case Enums.BlockDataObjectType.Invalid: //TODO: Check why this breaks sometimes?
                default:
                {
                    // For unit testing!
                    //if((int)packedObjectType != 1)
                    //    throw new Exception("Unknown type [" + (int)packedObjectType  + "] object!");
                    SBlockDataPackedObject = new CBytes(cr2w, this, nameof(SBlockDataPackedObject));
                    break;
                }
            }
        }

        public override void Read(BinaryReader file, uint size)
        {
            var startp = file.BaseStream.Position;

            base.Read(file, size);

            CreatePackedObject();
            
            SBlockDataPackedObject.Read(file, size - 56);
            SBlockDataPackedObject.SetIsSerialized();

            var endp = file.BaseStream.Position;
            var read = endp - startp;
            if (read < size)
            {
            }
            else if (read > size)
            {
                throw new InvalidParsingException("read too far");
            }
            //SetIsSerialized() in base
        }

        public override void Write(BinaryWriter file)
        {
            base.Write(file);

            if(SBlockDataPackedObject != null)
                SBlockDataPackedObject.Write(file);
        }

        public override string ToString()
        {
            return "Packed [" +
                            Enum.GetName(typeof(Enums.BlockDataObjectType), packedObjectType) + "] object";
        }

        public override CVariable Copy(CR2WCopyAction context)
        {
            if (SBlockDataPackedObject != null)
            {
                var copy = base.Copy(context) as SBlockData;
                switch (packedObjectType)
                {
                    //TODO: Add here the differnt copy methods
                    case Enums.BlockDataObjectType.Invalid:
                        {
                            //Empty
                            break;
                        }
                    case Enums.BlockDataObjectType.Mesh:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataMeshObject;
                            break;
                        }
                    case Enums.BlockDataObjectType.Collision:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataCollisionObject;
                            break;
                        }
                    case Enums.BlockDataObjectType.Decal:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataDecal;
                            break;
                        }                        
                    case Enums.BlockDataObjectType.Dimmer:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataDimmer;
                            break;
                        }
                    case Enums.BlockDataObjectType.SpotLight:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataSpotLight;
                            break;
                        }
                    case Enums.BlockDataObjectType.PointLight:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataLight;
                            break;
                        }
                    case Enums.BlockDataObjectType.Particles:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataParticles;
                            break;
                        }
                    case Enums.BlockDataObjectType.RigidBody:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as SBlockDataRigidBody;
                            break;
                        }
                    case Enums.BlockDataObjectType.Cloth:
                    case Enums.BlockDataObjectType.Destruction:
                    default:
                        {
                            copy.SBlockDataPackedObject = SBlockDataPackedObject.Copy(context) as CBytes;
                        }
                        break;
                }

                return copy;
            }
            else
                return base.Copy(context);
        }

        public override List<IEditableVariable> GetEditableVariables()
        {

            if (SBlockDataPackedObject != null)
            {
                var baseobj = base.GetEditableVariables();
                switch (packedObjectType)
                {
                    //case Enums.BlockDataObjectType.Invalid:
                    //    {
                    //        //Empty
                    //        break;
                    //    }
                    //TODO: Add here the differnt copy methods
                    
                    case Enums.BlockDataObjectType.Collision:
                        {
                            baseobj.Add((SBlockDataCollisionObject)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.Particles:
                        {
                            baseobj.Add((SBlockDataParticles)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.RigidBody:
                        {
                            baseobj.Add((SBlockDataRigidBody)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.Mesh:
                        {
                            baseobj.Add((SBlockDataMeshObject)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.Dimmer:
                        {
                            baseobj.Add((SBlockDataDimmer)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.PointLight:
                        {
                            baseobj.Add((SBlockDataLight)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.SpotLight:
                        {
                            baseobj.Add((SBlockDataSpotLight)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.Decal:
                        {
                            baseobj.Add((SBlockDataDecal)SBlockDataPackedObject);
                            break;
                        }
                    case Enums.BlockDataObjectType.Cloth:
                    case Enums.BlockDataObjectType.Destruction:
                    default:
                        {
                            baseobj.Add((CBytes)SBlockDataPackedObject);
                        }
                        break;
                }
                return baseobj;
            }                
            else
                return base.GetEditableVariables();
        }
    }
}
