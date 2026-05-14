using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using ServerCore;
using Server.Session;

namespace Server
{
    [Serializable]
    public class UserData
    {
        public List<S_PlayerDeckInfo.Card> cards = new List<S_PlayerDeckInfo.Card>();
    }

    [Serializable]
    [XmlRoot("UserDatas")]
    public class UserDatas
    {
        [Serializable]
        public class XmlKeyValuePair
        {
            [XmlAttribute("IP")]
            public string Key { get; set; }

            [XmlAttribute("Index")]
            public short Index { get; set; }

            [XmlElement("UserData")]
            public UserData Value { get; set; }

            public XmlKeyValuePair() { }
            public XmlKeyValuePair(string ip, short index, UserData userdata)
            {
                Key = ip;
                Index = index;
                Value = userdata;
            }
        }

        [XmlIgnore]
        public static readonly string PATH = "../../../UserData/";
        [XmlIgnore]
        public static UserDatas Instance = new UserDatas();

        [XmlArray("UserDataList"), XmlArrayItem("KeyValuePair", typeof(XmlKeyValuePair))]
        public List<XmlKeyValuePair> userdatas = new List<XmlKeyValuePair>();

        [XmlIgnore]
        public Dictionary<string, UserData> userdatasDict = new Dictionary<string, UserData>();

        public void Init()
        {
            try
            {
                using (var reader = new StreamReader(PATH + "userdata.xml"))
                {
                    XmlSerializer xs = new XmlSerializer(typeof(UserDatas));
                    UserDatas ud = xs.Deserialize(reader) as UserDatas;
                    userdatas = ud.userdatas;
                    UserDatasToDict();
                }
            }
            catch
            {
                CreateEmptyData(PATH);  // 맨 처음 서버 시작 시 데이터가 없을 때 임시 데이터 생성
            }
        }

        public void SaveData(string ip, UserData newUserdata, string path = "")
        {
            if (string.IsNullOrEmpty(path))
                path = PATH + "userdata.xml";

            if (userdatasDict.TryGetValue(ip, out UserData us) == false)
            {
                userdatasDict.Add(ip, newUserdata);
                userdatas.Add(new XmlKeyValuePair(ip, (short)userdatas.Count, newUserdata));
            }
            else
            {
                userdatasDict[ip] = newUserdata;
                userdatas[userdatas.Count - 1].Value = newUserdata;
            }
                

            using (var writer = new StreamWriter(path))
            {
                XmlSerializer xs = new XmlSerializer(typeof(UserDatas));
                xs.Serialize(writer, this);
            }
        }

        public UserData GetUserData(string ip)
        {
            if (userdatasDict.TryGetValue(ip, out UserData userData))
                return userData;
            else
            {
                Console.WriteLine("UserData doesn't exist.");
                return null;
            }
        }

        void CreateEmptyData(string path)
        {
            XmlDocument atXd = new XmlDocument();
            XmlDocument dfXd = new XmlDocument();
            atXd.Load(path + "attackCards.xml");
            dfXd.Load(path + "defenseCards.xml");
            XmlNodeList atList = atXd.GetElementsByTagName("card");
            XmlNodeList dfList = dfXd.GetElementsByTagName("card");

            UserData ud = new UserData();
            
            foreach (XmlNode node in atList)
            {
                S_PlayerDeckInfo.Card card = new S_PlayerDeckInfo.Card();
                card.cardId = Convert.ToInt16(node["cardId"].InnerText);
                ud.cards.Add(card);
            }

            foreach (XmlNode node in dfList)
            {
                S_PlayerDeckInfo.Card card = new S_PlayerDeckInfo.Card();
                card.cardId = Convert.ToInt16(node["cardId"].InnerText);
                ud.cards.Add(card);
            }

            XmlKeyValuePair kv = new XmlKeyValuePair("empty", 0, ud);
            userdatas.Add(kv);
            UserDatasToDict();
        }

        void UserDatasToDict()
        {
            foreach (XmlKeyValuePair kv in userdatas)
                userdatasDict.Add(kv.Key, kv.Value);
        }

        public void SendDeckPacket(UserData data, PacketSession session)
        {
            S_PlayerDeckInfo d = new S_PlayerDeckInfo();
            d.cards = data.cards;

            session.Send(d.Serialize());
        }
    }
}
