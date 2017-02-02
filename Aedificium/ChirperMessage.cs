using System;

namespace Aedificium
{
    public class ChirperMessage : MessageBase
    {
        public PrefabInfo Prefab
        {
            get; set;
        }
        
        public ChirperMessage( PrefabInfo prefab )
        {
            Prefab = prefab;
        }

        string prettyPrefabName()
        {
            return Prefab.name.reReplace( @"^\d+\.", "" ).reReplace( @"_Data$", "" );
        }

        string prettyAiName()
        {
            return Prefab.GetAI().GetType().Name.reReplace( @"^Rico", "" ).reReplace( @"AI$", "" );
        }


        public override uint GetSenderID()
        {
            return 0u;
        }

        public override string GetSenderName()
        {
            return "New workshop subscription!";
        }

        public override string GetText()
        {
            var text = String.Format(
                    "                           Name: {0}\r\n" +
                    "                           Class: {1}\r\n" +
                   "\r\n" + "\r\n" + "\r\n" + "\r\n",
                   prettyPrefabName(), Prefab is TreeInfo? "Tree" : "Prop"
                );

            if ( Prefab is BuildingInfo )
            {
                var b = (BuildingInfo) Prefab;

                text = String.Format(
                                "                           Name: {0}\r\n" +
                                "                           AI: {13}\r\n" +
                                "                           Class: {1}\r\n" +
                                "                           Category: {2}\r\n" +
                                "                           Area: {4}x{6} m ({3}x{5} u)\r\n" +
                                "                           Size: {7}x{9}x{11} m ({8}x{10}x{12} u)\r\n",
                                prettyPrefabName(),
                                b.m_class.name,
                                b.category,
                                b.m_cellLength,
                                b.m_cellLength * 8,
                                b.m_cellWidth,
                                b.m_cellWidth * 8,
                                (int) ( b.m_size.x ),
                                (int) ( b.m_size.x / 8 ),
                                (int) ( b.m_size.y ),
                                (int) ( b.m_size.y / 8 ),
                                (int) ( b.m_size.z ),
                                (int) ( b.m_size.z / 8 ), prettyAiName() );
            }
            return text;
        }
    }
}
