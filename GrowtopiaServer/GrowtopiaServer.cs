/**********************************************************************************
    Growtopia Private Server using ENet.Managed in C#.
    Copyright (C) 2018 willi123yao
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.
    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
**********************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ENet.Managed;
using Newtonsoft.Json.Linq;

namespace GrowtopiaServer
{
	class GrowtopiaServer
	{
		public static ENetHost server;
		public static int cId = 1;
		public static byte[] itemsDat;
		public static int itemsDatSize = 0;
		public static List<ENetPeer> peers = new List<ENetPeer>();
		public static WorldDB worldDB;
		public static ItemDefinition[] itemDefs = new ItemDefinition[] {};
		public DroppedItem[] droppedItems = new DroppedItem[] {};
		public static Admin[] admins = new Admin[] {};

		public static bool verifyPassword(string password, string hash)
		{
			return hashPassword(password) == hash;
		}

		public static string hashPassword(string password)
		{
			using (SHA256 sha256Hash = SHA256.Create())
			{
				byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < bytes.Length; i++)
				{
					builder.Append(bytes[i].ToString("x2"));
				}

				return builder.ToString();
			}
		}

		public static void sendData(ENetPeer peer, int num, byte[] data, int len)
		{
			byte[] packet = new byte[len + 5];
			Array.Copy(BitConverter.GetBytes(num), 0, packet, 0, 4);
			if (data != null)
			{
				Array.Copy(data, 0, packet, 4, len);
			}

			packet[4 + len] = 0;
			peer.Send(packet, 0, ENetPacketFlags.Reliable);
			server.Flush();
		}

		public int getPacketId(byte[] data)
		{
			return data[0];
		}

		public byte[] getPacketData(byte[] data)
		{
			return data.Skip(4).ToArray();
		}

		public string text_encode(string text)
		{
			string ret = "";
			int i = 0;
			while (text[i] != 0)
			{
				switch (text[i])
				{
					case '\n':
						ret += "\\n";
						break;
					case '\t':
						ret += "\\t";
						break;
					case '\b':
						ret += "\\b";
						break;
					case '\\':
						ret += "\\\\";
						break;
					case '\r':
						ret += "\\r";
						break;
					default:
						ret += text[i];
						break;
				}

				i++;
			}

			return ret;
		}

		public static byte ch2n(char x)
		{
			switch (x)
			{
				case '0':
					return 0;
				case '1':
					return 1;
				case '2':
					return 2;
				case '3':
					return 3;
				case '4':
					return 4;
				case '5':
					return 5;
				case '6':
					return 6;
				case '7':
					return 7;
				case '8':
					return 8;
				case '9':
					return 9;
				case 'A':
					return 10;
				case 'B':
					return 11;
				case 'C':
					return 12;
				case 'D':
					return 13;
				case 'E':
					return 14;
				case 'F':
					return 15;
			}

			return 0;
		}

		public static string[] explode(string delimiter, string str)
		{
			return str.Split(delimiter.ToCharArray());
		}

		public struct GamePacket
		{
			public byte[] data;
			public int len;
			public int indexes;
		}

		public static GamePacket appendFloat(GamePacket p, float val)
		{
			byte[] data = new byte[p.len + 2 + 4];
			Array.Copy(p.data, 0, data, 0, p.len);
			byte[] num = BitConverter.GetBytes(val);
			data[p.len] = (byte) p.indexes;
			data[p.len + 1] = 1;
			Array.Copy(num, 0, data, p.len + 2, 4);
			p.len = p.len + 2 + 4;
			p.indexes++;
			p.data = data;
			return p;
		}

		public static GamePacket appendFloat(GamePacket p, float val, float val2)
		{
			byte[] data = new byte[p.len + 2 + 8];
			Array.Copy(p.data, 0, data, 0, p.len);
			byte[] fl1 = BitConverter.GetBytes(val);
			byte[] fl2 = BitConverter.GetBytes(val2);
			data[p.len] = (byte) p.indexes;
			data[p.len + 1] = 3;
			Array.Copy(fl1, 0, data, p.len + 2, 4);
			Array.Copy(fl2, 0, data, p.len + 6, 4);
			p.len = p.len + 2 + 8;
			p.indexes++;
			p.data = data;
			return p;
		}

		public static GamePacket appendFloat(GamePacket p, float val, float val2, float val3)
		{
			byte[] data = new byte[p.len + 2 + 12];
			Array.Copy(p.data, 0, data, 0, p.len);
			byte[] fl1 = BitConverter.GetBytes(val);
			byte[] fl2 = BitConverter.GetBytes(val2);
			byte[] fl3 = BitConverter.GetBytes(val3);
			data[p.len] = (byte) p.indexes;
			data[p.len + 1] = 3;
			Array.Copy(fl1, 0, data, p.len + 2, 4);
			Array.Copy(fl2, 0, data, p.len + 6, 4);
			Array.Copy(fl3, 0, data, p.len + 10, 4);
			p.len = p.len + 2 + 12;
			p.indexes++;
			p.data = data;
			return p;
		}

		public static GamePacket appendInt(GamePacket p, Int32 val)
		{
			byte[] data = new byte[p.len + 2 + 4];
			Array.Copy(p.data, 0, data, 0, p.len);
			byte[] num = BitConverter.GetBytes(val);
			data[p.len] = (byte) p.indexes;
			data[p.len + 1] = 9;
			Array.Copy(num, 0, data, p.len + 2, 4);
			p.len = p.len + 2 + 4;
			p.indexes++;
			p.data = data;
			return p;
		}

		public static GamePacket appendIntx(GamePacket p, Int32 val)
		{
			byte[] data = new byte[p.len + 2 + 4];
			Array.Copy(p.data, 0, data, 0, p.len);
			byte[] num = BitConverter.GetBytes(val);
			data[p.len] = (byte) p.indexes;
			data[p.len + 1] = 5;
			Array.Copy(num, 0, data, p.len + 2, 4);
			p.len = p.len + 2 + 4;
			p.indexes++;
			p.data = data;
			return p;
		}

		public static GamePacket appendString(GamePacket p, string str)
		{
			byte[] data = new byte[p.len + 2 + str.Length + 4];
			Array.Copy(p.data, 0, data, 0, p.len);
			byte[] strn = Encoding.ASCII.GetBytes(str);
			data[p.len] = (byte) p.indexes;
			data[p.len + 1] = 2;
			byte[] len = BitConverter.GetBytes(str.Length);
			Array.Copy(len, 0, data, p.len + 2, 4);
			Array.Copy(strn, 0, data, p.len + 6, str.Length);
			p.len = p.len + 2 + str.Length + 4;
			p.indexes++;
			p.data = data;
			return p;
		}

		public static GamePacket createPacket()
		{
			byte[] data = new byte[61];
			string asdf = "0400000001000000FFFFFFFF00000000080000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
			for (int i = 0; i < asdf.Length; i += 2)
			{
				byte x = ch2n(asdf[i]);
				x = (byte)(x << 4);
				x += ch2n(asdf[i + 1]);
				data[i / 2] = x;
				if (asdf.Length > 61 * 2) throw new Exception("?");
			}
			GamePacket packet;
			packet.data = data;
			packet.len = 61;
			packet.indexes = 0;
			return packet;
		}

		public static GamePacket packetEnd(GamePacket p)
		{
			byte[] n = new byte[p.len + 1];
			Array.Copy(p.data, 0, n, 0, p.len);
			p.data = n;
			p.data[p.len] = 0;
			p.len += 1;
			p.data[56] = (byte) p.indexes;
			p.data[60] = (byte) p.indexes;
			//*(BYTE*)(p.data + 60) = p.indexes;
			return p;
		}
		
		public struct InventoryItem
		{
			public Int16 itemID;
			public byte itemCount;
		}
		
		public class PlayerInventory
		{
			public InventoryItem[] items = new InventoryItem[] {};
			public int inventorySize = 100;
		};

		public class PlayerInfo
		{
			public bool isIn = false;
			public int netID;
			public bool haveGrowId = false;
			public string tankIDName = "";
			public string tankIDPass = "";
			public string requestedName = "";
			public string rawName = "";
			public string displayName = "";
			public string country = "";
			public int adminLevel = 0;
			public string currentWorld = "EXIT";
			public bool radio = true;
			public int x;
			public int y;
			public bool isRotatedLeft = false;

			public bool isUpdating = false;
			public bool joinClothesUpdated = false;

			public int cloth_hair = 0; // 0
			public int cloth_shirt = 0; // 1
			public int cloth_pants = 0; // 2
			public int cloth_feet = 0; // 3
			public int cloth_face = 0; // 4
			public int cloth_hand = 0; // 5
			public int cloth_back = 0; // 6
			public int cloth_mask = 0; // 7
			public int cloth_necklace = 0; // 8
			
			public bool canWalkInBlocks = false; // 1
			public bool canDoubleJump = false; // 2
			public bool isInvisible = false; // 4
			public bool noHands = false; // 8
			public bool noEyes = false; // 16
			public bool noBody = false; // 32
			public bool devilHorns = false; // 64
			public bool goldenHalo = false; // 128
			public bool isFrozen = false; // 2048
			public bool isCursed = false; // 4096
			public bool isDuctaped = false; // 8192
			public bool haveCigar = false; // 16384
			public bool isShining = false; // 32768
			public bool isZombie = false; // 65536
			public bool isHitByLava = false; // 131072
			public bool haveHauntedShadows = false; // 262144
			public bool haveGeigerRadiation = false; // 524288
			public bool haveReflector = false; // 1048576
			public bool isEgged = false; // 2097152
			public bool havePineappleFloag = false; // 4194304
			public bool haveFlyingPineapple = false; // 8388608
			public bool haveSuperSupporterName = false; // 16777216
			public bool haveSupperPineapple = false; // 33554432
			//bool 
			public uint skinColor = 0x8295C3FF; //normal SKin color like gt!

			public PlayerInventory inventory = new PlayerInventory();

			public long lastSB = 0;
		}
		
		public static int getState(PlayerInfo info) {
			int val = 0;
			val |= info.canWalkInBlocks ? 1 : 0 << 0;
			val |= info.canDoubleJump ? 1 : 0 << 1;
			val |= info.isInvisible ? 1 : 0 << 2;
			val |= info.noHands ? 1 : 0 << 3;
			val |= info.noEyes ? 1 : 0 << 4;
			val |= info.noBody ? 1 : 0 << 5;
			val |= info.devilHorns ? 1 : 0 << 6;
			val |= info.goldenHalo ? 1 : 0 << 7;
			return val;
		}
		
		public struct WorldItem {
			public Int16 foreground;
			public Int16 background;
			public int breakLevel;
			public long breakTime;
			public bool water;
			public bool fire;
			public bool glue;
			public bool red;
			public bool green;
			public bool blue;
		};

		public class WorldInfo {
			public int width = 100;
			public int height = 60;
			public string name = "TEST";
			public WorldItem[] items;
			public string owner = "";
			public bool isPublic=false;
		};
		
		public static WorldInfo generateWorld(string name, int width, int height)
		{
			WorldInfo world = new WorldInfo();
			Random rand = new Random();
			world.name = name;
			world.width = width;
			world.height = height;
			world.items = new WorldItem[world.width*world.height];
			for (int i = 0; i < world.width*world.height; i++)
			{
				if (i >= 3800 && i<5400 && rand.Next(0, 50) == 0)
					world.items[i].foreground = 10;
				else if (i >= 3700 && i<5400)
				{
					world.items[i].foreground = 2;
				}
				else if (i >= 5400) {
					world.items[i].foreground = 8;
				}
				if (i >= 3700)
					world.items[i].background = 14;
				if (i == 3650)
					world.items[i].foreground = 6;
				else if (i >= 3600 && i<3700)
					world.items[i].foreground = 0; //fixed the grass in the world!
				if (i == 3750)
					world.items[i].foreground = 8;
			}
			return world;
		}
		
		public class PlayerDB {
			public static string getProperName(string name)
			{
				string newS = name.ToLower();
				var ret = new StringBuilder();
				for (int i = 0; i < newS.Length; i++)
				{
					if (newS[i] == '`') i++; else ret.Append(newS[i]);
				}

				var ret2 = new StringBuilder();
				foreach (char c in ret.ToString()) if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) ret2.Append(c);
				return ret2.ToString();
			}
			
			public static string fixColors(string text) {
				string ret = "";
				int colorLevel = 0;
				for (int i = 0; i < text.Length; i++)
				{
					if (text[i] == '`')
					{
						ret += text[i];
						if (i + 1 < text.Length)
							ret += text[i + 1];
						
						
						if (i+1 < text.Length && text[i + 1] == '`')
						{
							colorLevel--;
						}
						else {
							colorLevel++;
						}
						i++;
					} else {
						ret += text[i];
					}
				}
				for (int i = 0; i < colorLevel; i++) {
					ret += "``";
				}
				for (int i = 0; i > colorLevel; i--) {
					ret += "`w";
				}
				return ret;
			}
			
			public static int playerLogin(ENetPeer peer, string username, string password) {
				string path = "players/" + getProperName(username) + ".json";
				if (File.Exists(path)) {
					JObject j = JObject.Parse(File.ReadAllText(path));
					string pss = (string) j["password"];
					if (verifyPassword(password, pss)) {			
						foreach(ENetPeer currentPeer in peers)
						{
							if (currentPeer.State != ENetPeerState.Connected)
								continue;
							if (currentPeer == peer)
								continue;
							if ((currentPeer.Data as PlayerInfo).rawName == getProperName(username))
							{
								{
									GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Someone else logged into this account!"));
									currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable);
								}
								{
									GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "Someone else was logged into this account! He was kicked out now."));
									peer.Send(p.data, 0, ENetPacketFlags.Reliable);
								}
								//enet_host_flush(server);
								currentPeer.DisconnectLater(0);
							}
						}
						return 1;
					}
					else {
						return -1;
					}
				}
				else {
					return -2;
				}
			}
			
			public static int playerRegister(string username, string password, string passwordverify, string email, string discord) {
				username = getProperName(username);
				if (!discord.Contains("#") && discord.Length != 0) return -5;
				if (!email.Contains("@") && email.Length != 0) return -4;
				if (passwordverify != password) return -3;
				if (username.Length < 3) return -2;
				string path = "players/" + username + ".json";
				if (File.Exists(path)) {
					return -1;
				}
				
				JObject j = new JObject(
					new JProperty("username", username),
					new JProperty("password", hashPassword(password)),
					new JProperty("email", email),
					new JProperty("discord", discord),
					new JProperty("adminLevel", 0)
					);
				File.WriteAllText(path, j.ToString());
				return 1;
			}
		}
		
		public struct AWorld {
        	public WorldInfo info;
        	public int id;
        };
		
		public class WorldDB {
			public WorldInfo[] worlds = new WorldInfo[] {};
			
			public AWorld get2(string name) {
				if (worlds.Length > 200) {
					Console.WriteLine("Saving redundant worlds!");
					saveRedundant();
					Console.WriteLine("Redundant worlds are saved!");
				}

				AWorld ret = new AWorld();
				name = getStrUpper(name);
				if (name.Length < 1) throw new Exception("too short name"); // too short name
				foreach (char c in name) {
					if ((c<'A' || c>'Z') && (c<'0' || c>'9'))
						throw new Exception("bad name"); // wrong name
				}
				if (name == "EXIT") {
					throw new Exception("exit world");
				}
				for (int i = 0; i < worlds.Length; i++) {
					if (worlds[i].name == name)
					{
						ret.id = i;
						ret.info = worlds[i];
						return ret;
					}
				}

				string path = "worlds/" + name + ".json";
				if (File.Exists(path))
				{
					string contents = File.ReadAllText(path);
					JObject j = JObject.Parse(contents);
					WorldInfo info = new WorldInfo();
					info.name = (string)j["name"];
					info.width = (int)j["width"];
					info.height = (int)j["height"];
					info.owner = (string)j["owner"];
					info.isPublic = (bool)j["isPublic"];
					JArray tiles = (JArray)j["tiles"];
					int square = info.width*info.height;
					info.items = new WorldItem[square];
					for (int i = 0; i < square; i++) {
						info.items[i].foreground = (short)tiles[i]["fg"];
						info.items[i].background = (short)tiles[i]["bg"];
					}

					worlds = worlds.Append(info).ToArray();
					ret.id = worlds.Length - 1;
					ret.info = info;
					return ret;
				}
				else {
					WorldInfo info = generateWorld(name, 100, 60);
					worlds = worlds.Append(info).ToArray();
					ret.id = worlds.Length - 1;
					ret.info = info;
					return ret;
				}
			}
			
			public WorldInfo get(string name) {
				return get2(name).info;
			}
			
			public void flush(WorldInfo info)
			{
				string path = "worlds/" + info.name + ".json";
				JArray tiles = new JArray();
				int square = info.width*info.height;
				
				for (int i = 0; i < square; i++)
				{
					JObject tile = new JObject(
						new JProperty("fg", info.items[i].foreground),
						new JProperty("bg", info.items[i].background)
						);
					tiles.Add(tile);
				}
				JObject j = new JObject(
					new JProperty("name", info.name),
					new JProperty("width", info.width),
					new JProperty("height", info.height),
					new JProperty("owner", info.owner),
					new JProperty("isPublic", info.isPublic),
					new JProperty("tiles", tiles)
				);
				File.WriteAllText(path, j.ToString());
			}
			
			public void flush2(AWorld info)
			{
				flush(info.info);
			}
			
			public void save(AWorld info)
			{
				flush2(info);
				Array.Clear(worlds, info.id, 1);
			}
			
			public void saveAll()
			{
				for (int i = 0; i < worlds.Length; i++) {
					flush(worlds[i]);
				}
				worlds = new WorldInfo[] {};
			}
			
			public WorldInfo[] getRandomWorlds() {
				WorldInfo[] ret = new WorldInfo[] {};
				for (int i = 0; i < ((worlds.Length < 10) ? worlds.Length : 10); i++)
				{ // load first four worlds, it is excepted that they are special
					ret = ret.Append(worlds[i]).ToArray();
				}
				// and lets get up to 6 random
				if (worlds.Length > 4) {
					Random rand = new Random();
					for (int j = 0; j < 6; j++)
					{
						bool isPossible = true;
						WorldInfo world = worlds[rand.Next(0, worlds.Length - 4)];
						for (int i = 0; i < ret.Length; i++)
						{
							if (world.name == ret[i].name || world.name == "EXIT")
							{
								isPossible = false;
							}
						}
						if (isPossible)
							ret = ret.Append(world).ToArray();
					}
				}
				return ret;
			}

			public void saveRedundant()
			{
				for (int i = 4; i < worlds.Length; i++)
				{
					bool canBeFree = true;

					foreach (ENetPeer currentPeer in peers)
					{
						if (currentPeer.State != ENetPeerState.Connected) continue;
						if ((currentPeer.Data as PlayerInfo).currentWorld == worlds[i].name)
							canBeFree = false;
					}

					if (canBeFree)
					{
						flush(worlds[i]);
						Array.Clear(worlds, i, 1);
						i--;
					}
				}
			}
		}
        
        public static string getStrUpper(string txt) {
        	string ret = "";
        	foreach (char c in txt) ret += c.ToString().ToUpper();
        	return ret;
        }
		
		public static void saveAllWorlds() // atexit hack plz fix
		{
			Console.WriteLine("Saving worlds...");
			worldDB.saveAll();
			Console.WriteLine("Worlds saved!");
		}
		
		public static WorldInfo getPlyersWorld(ENetPeer peer)
		{
			try
			{
				return worldDB.get2((peer.Data as PlayerInfo).currentWorld).info;
			} catch {
				return null;
			}
		}

		public struct PlayerMoving {
			public int packetType;
			public int netID;
			public float x;
			public float y;
			public int characterState;
			public int plantingTree;
			public float XSpeed;
			public float YSpeed;
			public int punchX;
			public int punchY;
		};


		public enum ClothTypes {
			HAIR,
			SHIRT,
			PANTS,
			FEET,
			FACE,
			HAND,
			BACK,
			MASK,
			NECKLACE,
			NONE
		};

		public enum BlockTypes {
			FOREGROUND,
			BACKGROUND,
			SEED,
			PAIN_BLOCK,
			BEDROCK,
			MAIN_DOOR,
			SIGN,
			DOOR,
			CLOTHING,
			FIST,
			UNKNOWN
		};

		public struct ItemDefinition {
			public int id;
			public string name;
			public int rarity;
			public int breakHits;
			public int growTime;
			public ClothTypes clothType;
			public BlockTypes blockType;
			public  string description;
		}

		public struct DroppedItem { // TODO
			public int id;
			public int uid;
			public int count;
		};
		
		public static ItemDefinition getItemDef(int id)
		{
			if (id < itemDefs.Length && id > -1)
				return itemDefs[id];
			return itemDefs[0];
		}
		
		public struct Admin {
			public string username;
			public string password;
			public int level;
			public long lastSB;
		};
		
		public static void craftItemDescriptions()
		{
			if (!File.Exists("Descriptions.txt")) return;
			string contents = File.ReadAllText("Descriptions.txt");
			foreach (string line in contents.Split("\n".ToCharArray()))
			{
				if (line.Length > 3 && line[0] != '/' && line[1] != '/')
				{
					string[] ex = explode("|", line);
					if (Convert.ToInt32(ex[0]) + 1 < itemDefs.Length)
					{
						itemDefs[Convert.ToInt32(ex[0])].description = ex[1];
						if ((Convert.ToInt32(ex[0]) % 2) == 0)
							itemDefs[Convert.ToInt32(ex[0]) + 1].description = "This is a tree.";
					}
				}
			}
		}
		
		public static void buildItemsDatabase()
		{
			int current = -1;
			if (!File.Exists("CoreData.txt")) return;
			string contents = File.ReadAllText("CoreData.txt");
			foreach (string line in contents.Split("\n".ToCharArray()))
			{
				if (line.Length > 8 && line[0] != '/' && line[1] != '/')
				{
					string[] ex = explode("|", line);
					ItemDefinition def = new ItemDefinition();
					def.id = Convert.ToInt32(ex[0]);
					def.name = ex[1];
					def.rarity = Convert.ToInt32(ex[2]);
					string bt = ex[4];
					if (bt == "Foreground_Block") {
						def.blockType = BlockTypes.FOREGROUND;
					}
					else if(bt == "Seed") {
						def.blockType = BlockTypes.SEED;
					}
					else if (bt == "Pain_Block") {
						def.blockType = BlockTypes.PAIN_BLOCK;
					}
					else if (bt == "Main_Door") {
						def.blockType = BlockTypes.MAIN_DOOR;
					}
					else if (bt == "Bedrock") {
						def.blockType = BlockTypes.BEDROCK;
					}
					else if (bt == "Door") {
						def.blockType = BlockTypes.DOOR;
					}
					else if (bt == "Fist") {
						def.blockType = BlockTypes.FIST;
					}
					else if (bt == "Sign") {
						def.blockType = BlockTypes.SIGN;
					}
					else if (bt == "Background_Block") {
						def.blockType = BlockTypes.BACKGROUND;
					}
					else {
						def.blockType = BlockTypes.UNKNOWN;
					}
					def.breakHits = Convert.ToInt32(ex[7]);
					def.growTime = Convert.ToInt32(ex[8]);
					string cl = ex[9];
					if (cl == "None") {
						def.clothType = ClothTypes.NONE;
					}
					else if(cl == "Hat") {
						def.clothType = ClothTypes.HAIR;
					}
					else if(cl == "Shirt") {
						def.clothType = ClothTypes.SHIRT;
					}
					else if(cl == "Pants") {
						def.clothType = ClothTypes.PANTS;
					}
					else if (cl == "Feet") {
						def.clothType = ClothTypes.FEET;
					}
					else if (cl == "Face") {
						def.clothType = ClothTypes.FACE;
					}
					else if (cl == "Hand") {
						def.clothType = ClothTypes.HAND;
					}
					else if (cl == "Back") {
						def.clothType = ClothTypes.BACK;
					}
					else if (cl == "Hair") {
						def.clothType = ClothTypes.MASK;
					}
					else if (cl == "Chest") {
						def.clothType = ClothTypes.NECKLACE;
					}
					else {
						def.clothType = ClothTypes.NONE;
					}
					
					if (++current != def.id)
					{
						Console.WriteLine("Critical error! Unordered database at item " + current + "/" + def.id);
					}
		
					itemDefs = itemDefs.Append(def).ToArray();
				}
			}
			craftItemDescriptions();
		}
		
		public void addAdmin(string username, string password, int level)
		{
			Admin admin = new Admin();
			admin.username = username;
			admin.password = password;
			admin.level = level;
			admins = admins.Append(admin).ToArray();
		}

		public static int getAdminLevel(string username, string password) {
			for (int i = 0; i < admins.Length; i++) {
				Admin admin = admins[i];
				if (admin.username == username && admin.password == password) {
					return admin.level;
				}
			}
			return 0;
		}
		
		public static bool canSB(string username, string password) {
			for (int i = 0; i < admins.Length; i++) {
				Admin admin = admins[i];
				if (admin.username == username && admin.password == password && admin.level>1)
				{
					long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
					if (admin.lastSB + 900000 < time || admin.level == 999)
					{
						admins[i].lastSB = time;
						return true;
					}
				}
			}
			return false;
		}
		
		public bool canClear(string username, string password) {
			for (int i = 0; i < admins.Length; i++) {
				Admin admin = admins[i];
				if (admin.username == username && admin.password == password) {
					return admin.level > 0;
				}
			}
			return false;
		}

		public static bool isSuperAdmin(string username, string password) {
			for (int i = 0; i < admins.Length; i++) {
				Admin admin = admins[i];
				if (admin.username == username && admin.password == password && admin.level == 999) {
					return true;
				}
			}
			return false;
		}
		
		public static bool isHere(ENetPeer peer, ENetPeer peer2)
		{
			return ((peer.Data as PlayerInfo).currentWorld == (peer2.Data as PlayerInfo).currentWorld);
		}
		
		public static void sendInventory(ENetPeer peer, PlayerInventory inventory)
		{
			string asdf2 = "0400000009A7379237BB2509E8E0EC04F8720B050000000000000000FBBB0000010000007D920100FDFDFDFD04000000040000000000000000000000000000000000";
			int inventoryLen = inventory.items.Length;
			int packetLen = (asdf2.Length / 2) + (inventoryLen * 4) + 4;
			byte[] data2 = new byte[packetLen];
			for (int i = 0; i < asdf2.Length; i += 2)
			{
				byte x = ch2n(asdf2[i]);
				x = (byte)(x << 4);
				x += ch2n(asdf2[i + 1]);
				data2[i / 2] = x;
			}
			byte[] endianInvVal = BitConverter.GetBytes(inventoryLen);
			Array.Reverse(endianInvVal);
			Array.Copy(endianInvVal, 0, data2, asdf2.Length/2 - 4, 4);
			endianInvVal = BitConverter.GetBytes(inventory.inventorySize);
			Array.Reverse(endianInvVal);
			Array.Copy(endianInvVal, 0, data2, asdf2.Length/2 - 8, 4);
			int val = 0;
			for (int i = 0; i < inventoryLen; i++)
			{
				val = 0;
				val |= inventory.items[i].itemID;
				val |= inventory.items[i].itemCount << 16;
				val &= 0x00FFFFFF;
				val |= 0x00 << 24;
				byte[] value = BitConverter.GetBytes(val);
				Array.Copy(value, 0, data2, asdf2.Length/2 + (i*4), 4);
			}

			peer.Send(data2, 0, ENetPacketFlags.Reliable);
			//enet_host_flush(server);
		}
		
		public static byte[] packPlayerMoving(PlayerMoving dataStruct)
		{
			byte[] data = new byte[56];
			for (int i = 0; i < 56; i++)
			{
				data[i] = 0;
			}
			Array.Copy(BitConverter.GetBytes(dataStruct.packetType), 0, data, 0, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.netID), 0, data, 4, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.characterState), 0, data, 12, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.plantingTree), 0, data, 20, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.x), 0, data, 24, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.y), 0, data, 28, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.XSpeed), 0, data, 32, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.YSpeed), 0, data, 36, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.punchX), 0, data, 44, 4);
			Array.Copy(BitConverter.GetBytes(dataStruct.punchY), 0, data, 48, 4);
			return data;
		}
		
		public static PlayerMoving unpackPlayerMoving(byte[] data)
		{
			PlayerMoving dataStruct = new PlayerMoving();
			dataStruct.packetType = BitConverter.ToInt32(data, 0);
			dataStruct.netID = BitConverter.ToInt32(data, 4);
			dataStruct.characterState = BitConverter.ToInt32(data, 12);;
			dataStruct.plantingTree = BitConverter.ToInt32(data, 20);
			dataStruct.x = BitConverter.ToInt32(data, 24);
			dataStruct.y = BitConverter.ToInt32(data, 28);
			dataStruct.XSpeed = BitConverter.ToInt32(data, 32);
			dataStruct.YSpeed = BitConverter.ToInt32(data, 36);
			dataStruct.punchX = BitConverter.ToInt32(data, 44);
			dataStruct.punchY = BitConverter.ToInt32(data, 48);
			return dataStruct;
		}

		public void SendPacket(int a1, string a2, ENetPeer enetPeer)
		{
			if (enetPeer != null)
			{
				byte[] v3 = new byte[a2.Length + 5];
				Array.Copy(BitConverter.GetBytes(a1), 0, v3, 0, 4);
				//*(v3->data) = (DWORD)a1;
				Array.Copy(Encoding.ASCII.GetBytes(a2), 0, v3, 4, a2.Length);

				//cout << std::hex << (int)(char)v3->data[3] << endl;
				enetPeer.Send(v3, 0, ENetPacketFlags.Reliable);
			}
		}
		
		public static void SendPacketRaw(int a1, byte[] packetData, long packetDataSize, int a4, ENetPeer peer, int packetFlag)
		{
			if (peer != null) // check if we have it setup
			{
				if (a1 == 4 && (packetData[12] & 8) == 1)
				{
					byte[] p = new byte[packetDataSize + packetData[13]];
					Array.Copy(BitConverter.GetBytes(4), 0, p, 0, 4);
					Array.Copy(packetData, 0, p, 4, packetDataSize);
					Array.Copy(BitConverter.GetBytes(a4), 0, p, 4 + packetDataSize, 4);
					peer.Send(p, 0, ENetPacketFlags.Reliable);
				}
				else
				{
					byte[] p = new byte[packetDataSize + 5];
					Array.Copy(BitConverter.GetBytes(a1), 0, p, 0, 4);
					Array.Copy(packetData, 0, p, 4, packetDataSize);
					peer.Send(p, 0, ENetPacketFlags.Reliable);
				}
			}
		}
		
		public static void onPeerConnect(ENetPeer peer)
		{
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (peer != currentPeer)
				{
					if (isHere(peer, currentPeer))
					{
						string netIdS = (currentPeer.Data as PlayerInfo).netID.ToString();
						GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnSpawn"), "spawn|avatar\nnetID|" + netIdS + "\nuserID|" + netIdS + "\ncolrect|0|0|20|30\nposXY|" + (currentPeer.Data as PlayerInfo).x + "|" + (currentPeer.Data as PlayerInfo).y + "\nname|``" + (currentPeer.Data as PlayerInfo).displayName + "``\ncountry|" + (currentPeer.Data as PlayerInfo).country + "\ninvis|0\nmstate|0\nsmstate|0\n")); // ((PlayerInfo*)(server->peers[i].data))->tankIDName
						peer.Send(p.data, 0, ENetPacketFlags.Reliable);

						string netIdS2 = (peer.Data as PlayerInfo).netID.ToString();
						GamePacket p2 = packetEnd(appendString(appendString(createPacket(), "OnSpawn"), "spawn|avatar\nnetID|" + netIdS2 + "\nuserID|" + netIdS2 + "\ncolrect|0|0|20|30\nposXY|" + (currentPeer.Data as PlayerInfo).x + "|" + (currentPeer.Data as PlayerInfo).y + "\nname|``" + (currentPeer.Data as PlayerInfo).displayName + "``\ncountry|" + (currentPeer.Data as PlayerInfo).country + "\ninvis|0\nmstate|0\nsmstate|0\n")); // ((PlayerInfo*)(server->peers[i].data))->tankIDName
						peer.Send(p2.data, 0, ENetPacketFlags.Reliable);

						//enet_host_flush(server);
					}
				}
			}
		}
		
		public static void updateAllClothes(ENetPeer peer)
		{
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer))
				{
					GamePacket p3 = packetEnd(appendFloat(appendIntx(appendFloat(appendFloat(appendFloat(appendString(createPacket(), "OnSetClothing"), (peer.Data as PlayerInfo).cloth_hair, (peer.Data as PlayerInfo).cloth_shirt, (peer.Data as PlayerInfo).cloth_pants), (peer.Data as PlayerInfo).cloth_feet, (peer.Data as PlayerInfo).cloth_face, (peer.Data as PlayerInfo).cloth_hand), (peer.Data as PlayerInfo).cloth_back, (peer.Data as PlayerInfo).cloth_mask, (peer.Data as PlayerInfo).cloth_necklace), (int) (peer.Data as PlayerInfo).skinColor), 0.0f, 0.0f, 0.0f));
					Array.Copy(BitConverter.GetBytes((peer.Data as PlayerInfo).netID), 0, p3.data, 8, 4); // ffloor
	
					peer.Send(p3.data, 0, ENetPacketFlags.Reliable);
					//enet_host_flush(server);
					GamePacket p4 = packetEnd(appendFloat(appendIntx(appendFloat(appendFloat(appendFloat(appendString(createPacket(), "OnSetClothing"), (currentPeer.Data as PlayerInfo).cloth_hair, (currentPeer.Data as PlayerInfo).cloth_shirt, (currentPeer.Data as PlayerInfo).cloth_pants), (currentPeer.Data as PlayerInfo).cloth_feet, (currentPeer.Data as PlayerInfo).cloth_face, (currentPeer.Data as PlayerInfo).cloth_hand), (currentPeer.Data as PlayerInfo).cloth_back, (currentPeer.Data as PlayerInfo).cloth_mask, (currentPeer.Data as PlayerInfo).cloth_necklace), (int) (currentPeer.Data as PlayerInfo).skinColor), 0.0f, 0.0f, 0.0f));
					Array.Copy(BitConverter.GetBytes((currentPeer.Data as PlayerInfo).netID), 0, p3.data, 8, 4); // ffloor
					
					peer.Send(p4.data, 0, ENetPacketFlags.Reliable);
					//enet_host_flush(server);
				}
			}
		}
		
		public static void sendClothes(ENetPeer peer)
		{
			GamePacket p3 = packetEnd(appendFloat(appendIntx(appendFloat(appendFloat(appendFloat(appendString(createPacket(), "OnSetClothing"), (peer.Data as PlayerInfo).cloth_hair, (peer.Data as PlayerInfo).cloth_shirt, (peer.Data as PlayerInfo).cloth_pants), (peer.Data as PlayerInfo).cloth_feet, (peer.Data as PlayerInfo).cloth_face, (peer.Data as PlayerInfo).cloth_hand), (peer.Data as PlayerInfo).cloth_back, (peer.Data as PlayerInfo).cloth_mask, (peer.Data as PlayerInfo).cloth_necklace), (int) (peer.Data as PlayerInfo).skinColor), 0.0f, 0.0f, 0.0f));
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer))
				{
					Array.Copy(BitConverter.GetBytes((peer.Data as PlayerInfo).netID), 0, p3.data, 8, 4); // ffloor
					peer.Send(p3.data, 0, ENetPacketFlags.Reliable);
				}

			}
			//enet_host_flush(server);
		}
		
		public static void sendPData(ENetPeer peer, PlayerMoving data)
		{
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (peer != currentPeer)
				{
					if (isHere(peer, currentPeer))
					{
						data.netID = (peer.Data as PlayerInfo).netID;
						SendPacketRaw(4, packPlayerMoving(data), 56, 0, currentPeer, 0);
					}
				}
			}
		}
		
		public static int getPlayersCountInWorld(string name)
		{
			int count = 0;
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if ((currentPeer.Data as PlayerInfo).currentWorld == name)
					count++;
			}
			return count;
		}
		
		public static void sendRoulete(ENetPeer peer, int x, int y)
		{
			Random rand = new Random();
			int val = rand.Next(0, 37);
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer))
				{
					GamePacket p2 = packetEnd(appendIntx(appendString(appendIntx(appendString(createPacket(), "OnTalkBubble"), (peer.Data as PlayerInfo).netID), "`w[" + (peer.Data as PlayerInfo).displayName + " `wspun the wheel and got `6"+val+"`w!]"), 0));
					currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable);
				}
				
				//cout << "Tile update at: " << data2->punchX << "x" << data2->punchY << endl;
			}
		}
		
		public static void sendNothingHappened(ENetPeer peer, int x, int y) {
			PlayerMoving data = new PlayerMoving();
			data.netID = (peer.Data as PlayerInfo).netID;
			data.packetType = 0x8;
			data.plantingTree = 0;
			data.netID = -1;
			data.x = x;
			data.y = y;
			data.punchX = x;
			data.punchY = y;
			SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0);
		}
		
		public static void sendTileUpdate(int x, int y, int tile, int causedBy, ENetPeer peer)
		{
			PlayerMoving data;
			data.packetType = 0x3;
	
			data.characterState = 0x0; // animation
			data.x = x;
			data.y = y;
			data.punchX = x;
			data.punchY = y;
			data.XSpeed = 0;
			data.YSpeed = 0;
			data.netID = causedBy;
			data.plantingTree = tile;
			
			WorldInfo world = getPlyersWorld(peer);
			
			if (world == null) return;
			if (x<0 || y<0 || x>world.width || y>world.height) return;
			sendNothingHappened(peer,x,y);
			if (!isSuperAdmin((peer.Data as PlayerInfo).rawName, (peer.Data as PlayerInfo).tankIDPass))
			{
				if (world.items[x + (y*world.width)].foreground == 6 || world.items[x + (y*world.width)].foreground == 8 || world.items[x + (y*world.width)].foreground == 3760)
					return;
				if (tile == 6 || tile == 8 || tile == 3760)
					return;
			}
			if (world.name == "ADMIN" && getAdminLevel((peer.Data as PlayerInfo).rawName, (peer.Data as PlayerInfo).tankIDPass) == 0)
			{
				if (world.items[x + (y*world.width)].foreground == 758)
					sendRoulete(peer, x, y);
				return;
			}
			if (world.name != "ADMIN") {
				if (world.owner != "") {
					if ((peer.Data as PlayerInfo).rawName == world.owner) {
						// WE ARE GOOD TO GO
						if (tile == 32) {
							GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o\n\nadd_label_with_icon|big|`wShould this world be publicly breakable?``|left|242|\n\nadd_spacer|small|\nadd_button_with_icon|worldPublic|Public|noflags|2408||\nadd_button_with_icon|worldPrivate|Private|noflags|202||\nadd_spacer|small|\nadd_quick_exit|\nadd_button|chc0|Close|noflags|0|0|\nnend_dialog|gazette||OK|"));
							peer.Send(p.data, 0, ENetPacketFlags.Reliable);
	
							//enet_host_flush(server);
						}
					}
					else if (world.isPublic)
					{
						if (world.items[x + (y*world.width)].foreground == 242)
						{
							return;
						}
					}
					else {
						return;
					}
					if (tile == 242) {
						return;
					}
				}
			}
			if (tile == 32) {
				// TODO
				return;
			}
			if (tile == 822) {
				world.items[x + (y*world.width)].water = !world.items[x + (y*world.width)].water;
				return;
			}
			if (tile == 3062)
			{
				world.items[x + (y*world.width)].fire = !world.items[x + (y*world.width)].fire;
				return;
			}
			if (tile == 1866)
			{
				world.items[x + (y*world.width)].glue = !world.items[x + (y*world.width)].glue;
				return;
			}
			ItemDefinition def;
			try {
				def = getItemDef(tile);
				if (def.clothType != ClothTypes.NONE) return;
			}
			catch {
				def.breakHits = 4;
				def.blockType = BlockTypes.UNKNOWN;
			}
	
			if (tile == 544 || tile == 546 || tile == 4520 || tile == 382 || tile == 3116 || tile == 4520 || tile == 1792 || tile == 5666 || tile==2994 || tile==4368) return;
			if (tile == 5708 || tile == 5709 || tile == 5780 || tile == 5781 || tile == 5782 || tile == 5783 || tile == 5784 || tile == 5785 || tile == 5710 || tile == 5711 || tile == 5786 || tile == 5787 || tile == 5788 || tile == 5789 || tile == 5790 || tile == 5791 || tile == 6146 || tile == 6147 || tile == 6148 || tile == 6149 || tile == 6150 || tile == 6151 || tile == 6152 || tile == 6153 || tile == 5670 || tile == 5671 || tile == 5798 || tile == 5799 || tile == 5800 || tile == 5801 || tile == 5802 || tile == 5803 || tile == 5668 || tile == 5669 || tile == 5792 || tile == 5793 || tile == 5794 || tile == 5795 || tile == 5796 || tile == 5797 || tile == 544 || tile == 546 || tile == 4520 || tile == 382 || tile == 3116 || tile == 1792 || tile == 5666 || tile == 2994 || tile == 4368) return;
			if(tile == 1902 || tile == 1508 || tile == 428) return;
			if (tile == 410 || tile == 1770 || tile == 4720 || tile == 4882 || tile == 6392 || tile == 3212 || tile == 1832 || tile == 4742 || tile == 3496 || tile == 3270 || tile == 4722) return;
			if (tile >= 7068) return;
			if (tile == 0 || tile == 18) {
				//data.netID = -1;
				data.packetType = 0x8;
				data.plantingTree = 4;
				long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				//if (world->items[x + (y*world->width)].foreground == 0) return;
				if (time - world.items[x + (y*world.width)].breakTime >= 4000)
				{
					world.items[x + (y * world.width)].breakTime = time;
					world.items[x + (y*world.width)].breakLevel = 4; // TODO
					if (world.items[x + (y*world.width)].foreground == 758)
						sendRoulete(peer, x, y);
				}
				else
					if (y < world.height && world.items[x + (y*world.width)].breakLevel + 4 >= def.breakHits * 4) { // TODO
						data.packetType = 0x3;// 0xC; // 0xF // World::HandlePacketTileChangeRequest
						data.netID = -1;
						data.plantingTree = 0;
						world.items[x + (y*world.width)].breakLevel = 0;
						if (world.items[x + (y*world.width)].foreground != 0)
						{
							if (world.items[x + (y*world.width)].foreground == 242)
							{
								world.owner = "";
								world.isPublic = false;
							}
							world.items[x + (y*world.width)].foreground = 0;
						}
						else {
							world.items[x + (y*world.width)].background = 0;
						}
						
					}
					else
						if (y < world.height)
						{
							world.items[x + (y * world.width)].breakTime = time;
							world.items[x + (y*world.width)].breakLevel += 4; // TODO
							if (world.items[x + (y*world.width)].foreground == 758)
								sendRoulete(peer, x, y);
						}
	
			}
			else {
				for (int i = 0; i < (peer.Data as PlayerInfo).inventory.items.Length; i++)
				{
					if ((peer.Data as PlayerInfo).inventory.items[i].itemID == tile)
					{
						if ((uint)(peer.Data as PlayerInfo).inventory.items[i].itemCount>1)
						{
							(peer.Data as PlayerInfo).inventory.items[i].itemCount--;
						}
						else {
							Array.Clear((peer.Data as PlayerInfo).inventory.items, i, 1);
						}
					}
				}
				if (def.blockType == BlockTypes.BACKGROUND)
				{
					world.items[x + (y*world.width)].background = (short) tile;
				}
				else {
					world.items[x + (y*world.width)].foreground = (short) tile;
					if (tile == 242) {
						world.owner = (peer.Data as PlayerInfo).rawName;
						world.isPublic = false;
						
						foreach (ENetPeer currentPeer in peers)
						{
							if (currentPeer.State != ENetPeerState.Connected)
								continue;
							if (isHere(peer, currentPeer)) {
								GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`3[`w" + world.name + " `ohas been World Locked by `2" + (peer.Data as PlayerInfo).displayName + "`3]"));

								currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable);
							}
						}
					}
					
				}
	
				world.items[x + (y*world.width)].breakLevel = 0;
			}
	
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer))
					SendPacketRaw(4, packPlayerMoving(data), 56, 0, currentPeer, 0);
				
				//cout << "Tile update at: " << data2->punchX << "x" << data2->punchY << endl;
			}
		}
		
		public static void sendPlayerLeave(ENetPeer peer, PlayerInfo player)
		{
			GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnRemove"), "netID|" + player.netID + "\n")); // ((PlayerInfo*)(server->peers[i].data))->tankIDName
			GamePacket p2 = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`5<`w" + player.displayName + "`` left, `w" + getPlayersCountInWorld(player.currentWorld) + "`` others here>``"));
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer)) {
					{
					
						peer.Send(p.data, 0, ENetPacketFlags.Reliable);
					
					}
					{

						currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable);
					
					}
				}
			}
		}
		
		public static void sendChatMessage(ENetPeer peer, int netID, string message)
		{
			if (message.Length == 0) return;
			string name = "";
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if ((currentPeer.Data as PlayerInfo).netID == netID)
					name = (currentPeer.Data as PlayerInfo).displayName;

			}
			GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"), "`o<`w" + name + "`o> " + message));
			GamePacket p2 = packetEnd(appendIntx(appendString(appendIntx(appendString(createPacket(), "OnTalkBubble"), netID), message), 0));
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer))
				{
				
					currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable);
				
					//enet_host_flush(server);
				
					currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable);
				
					//enet_host_flush(server);
				}
			}
		}
		
		public static void sendWho(ENetPeer peer)
		{
			string name = "";
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer))
				{
					GamePacket p2 = packetEnd(appendIntx(appendString(appendIntx(appendString(createPacket(), "OnTalkBubble"), (currentPeer.Data as PlayerInfo).netID), (currentPeer.Data as PlayerInfo).displayName), 1));

					peer.Send(p2.data, 0, ENetPacketFlags.Reliable);
					//enet_host_flush(server);
				}
			}
		}
		
		public static void sendWorld(ENetPeer peer, WorldInfo worldInfo)
		{
			Console.WriteLine("Entering a world...");
			(peer.Data as PlayerInfo).joinClothesUpdated = false;
			string asdf = "0400000004A7379237BB2509E8E0EC04F8720B050000000000000000FBBB0000010000007D920100FDFDFDFD04000000040000000000000000000000070000000000"; // 0400000004A7379237BB2509E8E0EC04F8720B050000000000000000FBBB0000010000007D920100FDFDFDFD04000000040000000000000000000000080000000000000000000000000000000000000000000000000000000000000048133A0500000000BEBB0000070000000000
			string worldName = worldInfo.name;
			int xSize = worldInfo.width;
			int ySize = worldInfo.height;
			int square = xSize*ySize;
			Int16 nameLen = (short) worldName.Length;
			int payloadLen = asdf.Length / 2;
			int dataLen = payloadLen + 2 + nameLen + 12 + (square * 8) + 4;
			int allocMem = payloadLen + 2 + nameLen + 12 + (square * 8) + 4 + 16000;
			byte[] data = new byte[allocMem];
			for (int i = 0; i < asdf.Length; i += 2)
			{
				byte x = ch2n(asdf[i]);
				x = (byte) (x << 4);
				x += ch2n(asdf[i + 1]);
				data[i / 2] = x;
			}
			int zero = 0;
			Int16 item = 0;
			int smth = 0;
			for (int i = 0; i < square * 8; i += 4) Array.Copy(BitConverter.GetBytes(zero), 0, data, payloadLen + i + 14 + nameLen, 4);
			for (int i = 0; i < square * 8; i += 8) Array.Copy(BitConverter.GetBytes(item), 0, data, payloadLen + i + 14 + nameLen, 2);
			Array.Copy(BitConverter.GetBytes(nameLen), 0, data, payloadLen, 2);
			Array.Copy(Encoding.ASCII.GetBytes(worldName), 0, data, payloadLen + 2, nameLen);
			Array.Copy(BitConverter.GetBytes(xSize), 0, data, payloadLen + 2 + nameLen, 4);
			Array.Copy(BitConverter.GetBytes(ySize), 0, data, payloadLen + 6 + nameLen, 4);
			Array.Copy(BitConverter.GetBytes(square), 0, data, payloadLen + 10 + nameLen, 4);
			int blockPtr = payloadLen + 14 + nameLen;
			for (int i = 0; i < square; i++) {
				if ((worldInfo.items[i].foreground == 0) || (worldInfo.items[i].foreground == 2) || (worldInfo.items[i].foreground == 8) || (worldInfo.items[i].foreground == 100))
				{
					Array.Copy(BitConverter.GetBytes(worldInfo.items[i].foreground), 0, data, blockPtr, 2);
					long type = 0x00000000;
					// type 1 = locked
					if (worldInfo.items[i].water)
						type |= 0x04000000;
					if (worldInfo.items[i].glue)
						type |= 0x08000000;
					if (worldInfo.items[i].fire)
						type |= 0x10000000;
					if (worldInfo.items[i].red)
						type |= 0x20000000;
					if (worldInfo.items[i].green)
						type |= 0x40000000;
					if (worldInfo.items[i].blue)
						type |= 0x80000000;
	
					// int type = 0x04000000; = water
					// int type = 0x08000000 = glue
					// int type = 0x10000000; = fire
					// int type = 0x20000000; = red color
					// int type = 0x40000000; = green color
					// int type = 0x80000000; = blue color
					Array.Copy(BitConverter.GetBytes(type), 0, data, blockPtr + 4, 4);
				}
				else
				{
					Array.Copy(BitConverter.GetBytes(zero), 0, data, blockPtr, 2);
				}
				Array.Copy(BitConverter.GetBytes(worldInfo.items[i].background), 0, data, blockPtr + 2, 2);
				blockPtr += 8;
			}
			Array.Copy(BitConverter.GetBytes(smth), 0, data, dataLen - 4, 4);
			
			peer.Send(data, 0, ENetPacketFlags.Reliable);
			//enet_host_flush(server);
			for (int i = 0; i < square; i++) {
				if ((worldInfo.items[i].foreground == 0) || (worldInfo.items[i].foreground == 2) || (worldInfo.items[i].foreground == 8) || (worldInfo.items[i].foreground == 100))
					; // nothing
				else
				{
					PlayerMoving data1;
					//data.packetType = 0x14;
					data1.packetType = 0x3;
	
					//data.characterState = 0x924; // animation
					data1.characterState = 0x0; // animation
					data1.x = i%worldInfo.width;
					data1.y = i/worldInfo.height;
					data1.punchX = i%worldInfo.width;
					data1.punchY = i / worldInfo.width;
					data1.XSpeed = 0;
					data1.YSpeed = 0;
					data1.netID = -1;
					data1.plantingTree = worldInfo.items[i].foreground;
					SendPacketRaw(4, packPlayerMoving(data1), 56, 0, peer, 0);
				}
			}
			(peer.Data as PlayerInfo).currentWorld = worldInfo.name;	
		}
		
		public static void sendAction(ENetPeer peer, int netID, string action)
		{
			string name = "";
			GamePacket p2 = packetEnd(appendString(appendString(createPacket(), "OnAction"), action));
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer)) {
					
					Array.Copy(BitConverter.GetBytes(netID), 0, p2.data, 8, 4);
					
					currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable);
					//enet_host_flush(server);
				}
			}
		}
		
		// droping items WorldObjectMap::HandlePacket
		public static void sendDrop(ENetPeer peer, int netID, int x, int y, int item, int count, byte specialEffect)
		{
			if (item >= 7068) return;
			if (item < 0) return;
			string name = "";
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer)) {
					PlayerMoving data = new PlayerMoving();
					data.packetType = 14;
					data.x = x;
					data.y = y;
					data.netID = netID;
					data.plantingTree = item;
					float val = count; // item count
					byte val2 = specialEffect;

					byte[] raw = packPlayerMoving(data);
					Array.Copy(BitConverter.GetBytes(val), 0, raw, 16, 4);
					Array.Copy(BitConverter.GetBytes(val2), 0, raw, 1, 1);

					SendPacketRaw(4, raw, 56, 0, currentPeer, 0);
				}
			}
		}
		
		public static void sendState(ENetPeer peer) {
			//return; // TODO
			PlayerInfo info = peer.Data as PlayerInfo;
			int netID = info.netID;
			int state = getState(info);
			
			foreach (ENetPeer currentPeer in peers)
			{
				if (currentPeer.State != ENetPeerState.Connected)
					continue;
				if (isHere(peer, currentPeer)) {
					PlayerMoving data;
					data.packetType = 0x14;
					data.characterState = 0; // animation
					data.x = 1000;
					data.y = 100;
					data.punchX = 0;
					data.punchY = 0;
					data.XSpeed = 300;
					data.YSpeed = 600;
					data.netID = netID;
					data.plantingTree = state;
					byte[] raw = packPlayerMoving(data);
					int var = 0x808000; // placing and breking
					Array.Copy(BitConverter.GetBytes(var), 0, raw, 1, 3);
					SendPacketRaw(4, raw, 56, 0, currentPeer, 0);
				}
			}
			// TODO
		}
		
		public static void sendWorldOffers(ENetPeer peer)
		{
			if (!(peer.Data as PlayerInfo).isIn) return;
			WorldInfo[] worldz = worldDB.getRandomWorlds();
			string worldOffers = "default|";
			if (worldz.Length > 0) {
				worldOffers += worldz[0].name;
			}
		
			worldOffers += "\nadd_button|Showing: `wWorlds``|_catselect_|0.6|3529161471|\n";
			for (int i = 0; i < worldz.Length; i++) {
				worldOffers += "add_floater|"+worldz[i].name+"|"+getPlayersCountInWorld(worldz[i].name)+"|0.55|3529161471\n";
			}
			GamePacket p3 = packetEnd(appendString(appendString(createPacket(), "OnRequestWorldSelectMenu"), worldOffers));
			peer.Send(p3.data, 0, ENetPacketFlags.Reliable);
		}

		public static void HandlerRoutine(object sender, ConsoleCancelEventArgs e)
		{
			saveAllWorlds();
			Environment.Exit(0);
		}

		static void Main(string[] args)
		{
			Console.WriteLine("Growtopia private server (c) willi12yao");
			ManagedENet.Startup();
		
			Console.CancelKeyPress += HandlerRoutine;
			
			// load items.dat
			if (File.Exists("items.dat")) {
				byte[] itemsData = File.ReadAllBytes("items.dat");
				itemsDatSize = itemsData.Length;
		
			
				itemsDat = new byte[60 + itemsDatSize];
				string asdf = "0400000010000000FFFFFFFF000000000800000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
				for (int i = 0; i < asdf.Length; i += 2)
				{
					byte x = ch2n(asdf[i]);
					x = (byte) (x << 4);
					x += ch2n(asdf[i + 1]);
					itemsDat[i / 2] = x;
					if (asdf.Length > 60 * 2) throw new Exception("Error");
				}
				Array.Copy(BitConverter.GetBytes(itemsDatSize), 0, itemsDat, 56, 4);
		
				Console.WriteLine("Updating item data success!");
			}
			else {
				Console.WriteLine("Updating item data failed");
			}
			
			worldDB = new WorldDB();
		
			//world = generateWorld();
			worldDB.get("TEST");
			worldDB.get("MAIN");
			worldDB.get("NEW");
			worldDB.get("ADMIN");
			IPEndPoint address = new IPEndPoint(IPAddress.Any, 17091);
			
			server = new ENetHost(address, 1024, 10);
			server.ChecksumWithCRC32();
			server.CompressWithRangeCoder();
		
			Console.WriteLine("Building items database...");
			buildItemsDatabase();
			Console.WriteLine("Database is built!");

			server.OnConnect += (object sender, ENetConnectEventArgs eve) =>
			{
				{
					ENetPeer peer = eve.Peer;
					Console.WriteLine("A new client connected.");
					//event.peer->data = "Client information";
					int count = 0;

					foreach (ENetPeer currentPeer in peers)
					{
						if (currentPeer.State != ENetPeerState.Connected)
							continue;
						if (currentPeer.RemoteEndPoint.Equals(peer.RemoteEndPoint))
							count++;
					}

					peer.Data = new PlayerInfo();
					if (count > 3)
					{
						GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"),
							"`rToo many accounts are logged on from this IP. Log off one account before playing please.``"));
						peer.Send(p.data, 0, ENetPacketFlags.Reliable);
						//enet_host_flush(server);
						peer.DisconnectLater(0);
					}
					else
					{
						sendData(peer, 1, BitConverter.GetBytes(0), 0);
						peers.Add(peer);
					}
				}

				eve.Peer.OnReceive += (object send, ENetPacket ev) =>
				{
					byte[] pak = ev.GetPayloadCopy();
					ENetPeer peer = send as ENetPeer;
					if ((peer.Data as PlayerInfo).isUpdating)
					{
						Console.WriteLine("packet drop");
						return;
					}

					int messageType = pak[0];
					//cout << "Packet type is " << messageType << endl;
					//cout << (event->packet->data+4) << endl;
					WorldInfo world = getPlyersWorld(peer);
					switch (messageType)
					{
						case 2:
						{
							//cout << GetTextPointerFromPacket(event.packet) << endl;
							string cch = Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray());
							if (cch.IndexOf("action|respawn") == 0)
							{
								{
									int x = 3040;
									int y = 736;

									if (world == null) return;

									for (int i = 0; i < world.width * world.height; i++)
									{
										if (world.items[i].foreground == 6)
										{
											x = (i % world.width) * 32;
											y = (i / world.width) * 32;
										}
									}	

									PlayerMoving data;
									data.packetType = 0x0;
									data.characterState = 0x924; // animation
									data.x = x;
									data.y = y;
									data.punchX = -1;
									data.punchY = -1;
									data.XSpeed = 0;
									data.YSpeed = 0;
									data.netID = (peer.Data as PlayerInfo).netID;
									data.plantingTree = 0x0;
									SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0);
								}

								{
									int x = 3040;
									int y = 736;

									for (int i = 0; i < world.width * world.height; i++)
									{
										if (world.items[i].foreground == 6)
										{
											x = (i % world.width) * 32;
											y = (i / world.width) * 32;
										}
									}

									GamePacket p2 = packetEnd(appendFloat(appendString(createPacket(), "OnSetPos"), x,
										y));
									Array.Copy(BitConverter.GetBytes((peer.Data as PlayerInfo).netID), 0, p2.data, 8, 4);

									peer.Send(p2.data, 0, ENetPacketFlags.Reliable);
									//enet_host_flush(server);
								}
								{
									int x = 3040;
									int y = 736;

									for (int i = 0; i < world.width * world.height; i++)
									{
										if (world.items[i].foreground == 6)
										{
											x = (i % world.width) * 32;
											y = (i / world.width) * 32;
										}
									}

									GamePacket p2 =
										packetEnd(appendIntx(appendString(createPacket(), "OnSetFreezeState"), 0));
									Array.Copy(BitConverter.GetBytes((peer.Data as PlayerInfo).netID), 0, p2.data, 8, 4);

									peer.Send(p2.data, 0, ENetPacketFlags.Reliable);
									//enet_host_flush(server);
								}
								Console.WriteLine("Respawning");
							}

							if (cch.IndexOf("action|growid") == 0)
							{
								//GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o\n\nadd_label_with_icon|big|`w" + itemDefs.at(id).name + "``|left|" + std::to_string(id) + "|\n\nadd_spacer|small|\nadd_textbox|" + itemDefs.at(id).description + "|left|\nadd_spacer|small|\nadd_quick_exit|\nadd_button|chc0|Close|noflags|0|0|\nnend_dialog|gazette||OK|"));
								GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"),
									"set_default_color|`o\n\nadd_label_with_icon|big|`wGet a GrowID``|left|206|\n\nadd_spacer|small|\nadd_textbox|A `wGrowID `wmeans `oyou can use a name and password to logon from any device.|\nadd_spacer|small|\nadd_textbox|This `wname `owill be reserved for you and `wshown to other players`o, so choose carefully!|\nadd_text_input|username|GrowID||30|\nadd_text_input|password|Password||100|\nadd_text_input|passwordverify|Password Verify||100|\nadd_textbox|Your `wemail address `owill only be used for account verification purposes and won't be spammed or shared. If you use a fake email, you'll never be able to recover or change your password.|\nadd_text_input|email|Email||100|\nadd_textbox|Your `wDiscord ID `owill be used for secondary verification if you lost access to your `wemail address`o! Please enter in such format: `wdiscordname#tag`o. Your `wDiscord Tag `ocan be found in your `wDiscord account settings`o.|\nadd_text_input|discord|Discord||100|\nend_dialog|register|Cancel|Get My GrowID!|\n"));
								peer.Send(p.data, 0, ENetPacketFlags.Reliable);
							}

							if (cch.IndexOf("action|store") == 0)
							{
								GamePacket p2 = packetEnd(appendString(appendString(createPacket(), "OnStoreRequest"),
									"set_description_text|Welcome to the `2Growtopia Store``!  Tap the item you'd like more info on.`o  `wWant to get `5Supporter`` status? Any Gem purchase (or `57,000`` Gems earned with free `5Tapjoy`` offers) will make you one. You'll get new skin colors, the `5Recycle`` tool to convert unwanted items into Gems, and more bonuses!\nadd_button|iap_menu|Buy Gems|interface/large/store_buttons5.rttex||0|2|0|0||\nadd_button|subs_menu|Subscriptions|interface/large/store_buttons22.rttex||0|1|0|0||\nadd_button|token_menu|Growtoken Items|interface/large/store_buttons9.rttex||0|0|0|0||\nadd_button|pristine_forceps|`oAnomalizing Pristine Bonesaw``|interface/large/store_buttons20.rttex|Built to exacting specifications by GrowTech engineers to find and remove temporal anomalies from infected patients, and with even more power than Delicate versions! Note : The fragile anomaly - seeking circuitry in these devices is prone to failure and may break (though with less of a chance than a Delicate version)! Use with care!|0|3|3500|0||\nadd_button|itemomonth|`oItem Of The Month``|interface/large/store_buttons16.rttex|`2September 2018:`` `9Sorcerer's Tunic of Mystery!`` Capable of reflecting the true colors of the world around it, this rare tunic is made of captured starlight and aether. If you think knitting with thread is hard, just try doing it with moonbeams and magic! The result is worth it though, as these clothes won't just make you look amazing - you'll be able to channel their inherent power into blasts of cosmic energy!``|0|3|200000|0||\nadd_button|contact_lenses|`oContact Lens Pack``|interface/large/store_buttons22.rttex|Need a colorful new look? This pack includes 10 random Contact Lens colors (and may include Contact Lens Cleaning Solution, to return to your natural eye color)!|0|7|15000|0||\nadd_button|locks_menu|Locks And Stuff|interface/large/store_buttons3.rttex||0|4|0|0||\nadd_button|itempack_menu|Item Packs|interface/large/store_buttons3.rttex||0|3|0|0||\nadd_button|bigitems_menu|Awesome Items|interface/large/store_buttons4.rttex||0|6|0|0||\nadd_button|weather_menu|Weather Machines|interface/large/store_buttons5.rttex|Tired of the same sunny sky?  We offer alternatives within...|0|4|0|0||\n"));
								peer.Send(p2.data, 0, ENetPacketFlags.Reliable);
								//enet_host_flush(server);
							}

							if (cch.IndexOf("action|info") == 0)
							{
								int id = -1;
								int count = -1;
								foreach(string to in cch.Split("\n".ToCharArray()))
								{
									string[] infoDat = explode("|", to);
									if (infoDat.Length == 3)
									{
										if (infoDat[1] == "itemID") id = Convert.ToInt32(infoDat[2]);
										if (infoDat[1] == "count") count = Convert.ToInt32(infoDat[2]);
									}
								}

								if (id == -1 || count == -1) return;
								if (itemDefs.Length < id || id < 0) return;
								GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"),
									"set_default_color|`o\n\nadd_label_with_icon|big|`w" + itemDefs[id].name +
									"``|left|" + id + "|\n\nadd_spacer|small|\nadd_textbox|" +
									itemDefs[id].description +
									"|left|\nadd_spacer|small|\nadd_quick_exit|\nadd_button|chc0|Close|noflags|0|0|\nnend_dialog|gazette||OK|"));
								peer.Send(p.data, 0, ENetPacketFlags.Reliable);

								//enet_host_flush(server);
			
							}

							if (cch.IndexOf("action|dialog_return") == 0)
							{
								string btn = "";
								bool isRegisterDialog = false;
								string username = "";
								string password = "";
								string passwordverify = "";
								string email = "";
								string discord = "";
								foreach(string to in cch.Split("\n".ToCharArray()))
								{
									string[] infoDat = explode("|", to);
									if (infoDat.Length == 2)
									{
										if (infoDat[0] == "buttonClicked") btn = infoDat[1];
										if (infoDat[0] == "dialog_name" && infoDat[1] == "register")
										{
											isRegisterDialog = true;
										}

										if (isRegisterDialog)
										{
											if (infoDat[0] == "username") username = infoDat[1];
											if (infoDat[0] == "password") password = infoDat[1];
											if (infoDat[0] == "passwordverify") passwordverify = infoDat[1];
											if (infoDat[0] == "email") email = infoDat[1];
											if (infoDat[0] == "discord") discord = infoDat[1];
										}
									}
								}

								if (btn == "worldPublic")
									if ((peer.Data as PlayerInfo).rawName == getPlyersWorld(peer).owner)
										getPlyersWorld(peer).isPublic = true;
								if (btn == "worldPrivate")
									if ((peer.Data as PlayerInfo).rawName == getPlyersWorld(peer).owner)
										getPlyersWorld(peer).isPublic = false;
								if (isRegisterDialog)
								{

									int regState = PlayerDB.playerRegister(username, password, passwordverify, email,
										discord);
									if (regState == 1)
									{
										GamePacket p = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`rYour account has been created!``"));
										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
										
										GamePacket p2 = packetEnd(appendString(
											appendString(appendInt(appendString(createPacket(), "SetHasGrowID"), 1),
												username), password));
										peer.Send(p2.data, 0, ENetPacketFlags.Reliable);

										//enet_host_flush(server);
										peer.DisconnectLater(0);
									}
									else if (regState == -1)
									{
										GamePacket p = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`rAccount creation has failed, because it already exists!``"));
										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}
									else if (regState == -2)
									{
										GamePacket p = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`rAccount creation has failed, because the name is too short!``"));
										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}
									else if (regState == -3)
									{
										GamePacket p = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`4Passwords mismatch!``"));
										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}
									else if (regState == -4)
									{
										GamePacket p = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`4Account creation has failed, because email address is invalid!``"));
										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}
									else if (regState == -5)
									{
										GamePacket p = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`4Account creation has failed, because Discord ID is invalid!``"));
										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}
								}
							}

							string dropText = "action|drop\n|itemID|";
							if (cch.IndexOf(dropText) == 0)
							{
								sendDrop(peer, -1,
									(peer.Data as PlayerInfo).x +
									(32 * ((peer.Data as PlayerInfo).isRotatedLeft ? -1 : 1)),
									(peer.Data as PlayerInfo).y,
									Convert.ToInt32(cch.Substring(dropText.Length, cch.Length - dropText.Length - 1)),
									1, 0);
							}

							if (cch.Contains("text|")) {
								string str = cch.Substring(cch.IndexOf("text|") + 5, cch.Length - cch.IndexOf("text|") - 1);
								if (str == "/mod")
								{
									(peer.Data as PlayerInfo).canWalkInBlocks = true;
									sendState(peer);
								}
								else if (str.Substring(0, 7) == "/state ")
								{
									PlayerMoving data;
									data.packetType = 0x14;
									data.characterState = 0x0; // animation
									data.x = 1000;
									data.y = 0;
									data.punchX = 0;
									data.punchY = 0;
									data.XSpeed = 300;
									data.YSpeed = 600;
									data.netID = (peer.Data as PlayerInfo).netID;
									data.plantingTree = Convert.ToInt32(str.Substring(7, cch.Length - 7 - 1));
									SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0);
								}
								else if (str == "/help")
								{
									GamePacket p = packetEnd(appendString(
										appendString(createPacket(), "OnConsoleMessage"),
										"Supported commands are: /help, /mod, /unmod, /inventory, /item id, /team id, /color number, /who, /state number, /count, /sb message, /alt, /radio, /gem"));
									
									peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									//enet_host_flush(server);
								}
								else if (str.Substring(0, 5) == "/gem ") //gem if u want flex with ur gems!
								{
									GamePacket p = packetEnd(appendInt(appendString(createPacket(), "OnSetBux"),
										Convert.ToInt32(str.Substring(5))));
									peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									return;

								}
								else if (str.Substring(0, 9) == "/weather ")
								{
									if (world.name != "ADMIN")
									{
										if (world.owner != "")
										{
											if ((peer.Data as PlayerInfo).rawName == world.owner ||
											    isSuperAdmin((peer.Data as PlayerInfo).rawName,
												    (peer.Data as PlayerInfo).tankIDPass))

											{
												
												foreach (ENetPeer currentPeer in peers)
												{
													if (currentPeer.State != ENetPeerState.Connected)
														continue;
													if (isHere(peer, currentPeer))
													{
														GamePacket p1 = packetEnd(
															appendString(
																appendString(createPacket(), "OnConsoleMessage"),
																"`oPlayer `2" +
																(peer.Data as PlayerInfo).displayName +
																"`o has just changed the world's weather!"));
														currentPeer.Send(p1.data, 0, ENetPacketFlags.Reliable);

														GamePacket p2 = packetEnd(
															appendInt(
																appendString(createPacket(), "OnSetCurrentWeather"),
																Convert.ToInt32(str.Substring(9))));
														currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable);
													}
												}
											}
										}
									}
								}
								else if (str == "/count")
								{
									int count = 0;
									string name = "";
									
									foreach (ENetPeer currentPeer in peers)
									{
										if (currentPeer.State != ENetPeerState.Connected)
											continue;
										count++;
									}

									GamePacket p = packetEnd(appendString(
										appendString(createPacket(), "OnConsoleMessage"),
										"There are " + count + " people online out of 1024 limit."));

									peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									//enet_host_flush(server);
								}
								else if (str.Substring(0, 5) == "/asb ")
								{
									if (!canSB((peer.Data as PlayerInfo).rawName, (peer.Data as PlayerInfo).tankIDPass)) return;
									Console.WriteLine("ASB from "+(peer.Data as PlayerInfo).rawName+" in world "
									                  +(peer.Data as PlayerInfo).currentWorld+" with IP " 
									                  +peer.RemoteEndPoint.ToString()+" with message "
									                  +str.Substring(5, cch.Length - 5 - 1));
									GamePacket p = packetEnd(appendInt(
										appendString(
											appendString(
												appendString(appendString(createPacket(), "OnAddNotification"),
													"interface/atomic_button.rttex"),
												str.Substring(4, cch.Length - 4 - 1)), "audio/hub_open.wav"),
										0));
									
									foreach (ENetPeer currentPeer in peers)
									{
										if (currentPeer.State != ENetPeerState.Connected)
											continue;
										currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}

									//enet_host_flush(server);
								}
								else if (str.Substring(0, 4) == "/sb ")
								{
									long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
									if ((peer.Data as PlayerInfo).lastSB + 45000 < time)
									{
										(peer.Data as PlayerInfo).lastSB = time;
									}
									else
									{
										GamePacket p1 = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"Wait a minute before using the SB command again!"));
										
										peer.Send(p1.data, 0, ENetPacketFlags.Reliable);
										//enet_host_flush(server);
										return;
									}

									string name = (peer.Data as PlayerInfo).displayName;
									GamePacket p2 = packetEnd(appendString(
										appendString(createPacket(), "OnConsoleMessage"),
										"`w** `5Super-Broadcast`` from `$`2" + name + "```` (in `$" +
										(peer.Data as PlayerInfo).currentWorld + "``) ** :`` `# " +
										str.Substring(4, cch.Length - 4 - 1)));
									string text = "action|play_sfx\nfile|audio/beep.wav\ndelayMS|0\n";
									byte[] data = new byte[5 + text.Length];
									int zero = 0;
									int type = 3;
									Array.Copy(BitConverter.GetBytes(type), 0, data, 0, 4);
									Array.Copy(Encoding.ASCII.GetBytes(text), 0, data, 4, text.Length);
									Array.Copy(BitConverter.GetBytes(zero), 0, data, 4 + text.Length, 1);
									
									foreach (ENetPeer currentPeer in peers)
									{
										if (currentPeer.State != ENetPeerState.Connected)
											continue;
										if (!(peer.Data as PlayerInfo).radio)
											continue;
										
										currentPeer.Send(p2.data, 0, ENetPacketFlags.Reliable);
										currentPeer.Send(data, 0, ENetPacketFlags.Reliable);

										//enet_host_flush(server);
									}
								}
								else if (str.Substring(0, 6) == "/radio")
								{
									GamePacket p;
									if ((peer.Data as PlayerInfo).radio)
									{
										p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"),
											"You won't see broadcasts anymore."));
										(peer.Data as PlayerInfo).radio = false;
									}
									else
									{
										p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"),
											"You will now see broadcasts again."));
										(peer.Data as PlayerInfo).radio = true;
									}

									peer.Send(p.data, 0, ENetPacketFlags.Reliable);
									//enet_host_flush(server);
								}
								else if (str.Substring(0, 6) == "/reset")
								{
									if (!isSuperAdmin((peer.Data as PlayerInfo).rawName,
										(peer.Data as PlayerInfo).tankIDPass)) break;
									
									Console.WriteLine("Restart from "+(peer.Data as PlayerInfo).displayName);
									GamePacket p = packetEnd(appendInt(
										appendString(
											appendString(
												appendString(appendString(createPacket(), "OnAddNotification"),
													"interface/science_button.rttex"), "Restarting soon!"),
											"audio/mp3/suspended.mp3"), 0));
									
									foreach (ENetPeer currentPeer in peers)
									{
										if (currentPeer.State != ENetPeerState.Connected)
											continue;
										currentPeer.Send(p.data, 0, ENetPacketFlags.Reliable);
									}

									//enet_host_flush(server);
								}
								else if (str == "/unmod")
								{
									(peer.Data as PlayerInfo).canWalkInBlocks = false;
									sendState(peer);
								}
								else if (str == "/alt")
								{
									GamePacket p2 = packetEnd(appendInt(appendString(createPacket(), "OnSetBetaMode"),
										1));
									peer.Send(p2.data, 0, ENetPacketFlags.Reliable);
									//enet_host_flush(server);
								}
								else if (str == "/inventory")
								{
									sendInventory(peer, (peer.Data as PlayerInfo).inventory);
								}
								else if (str.Substring(0, 6) == "/item ")
								{
									PlayerInventory inventory = new PlayerInventory();
									InventoryItem item;
									item.itemID = Convert.ToInt16(str.Substring(6, cch.Length - 6 - 1));
									item.itemCount = 200;
									inventory.items = inventory.items.Append(item).ToArray();
									item.itemCount = 1;
									item.itemID = 18;
									inventory.items = inventory.items.Append(item).ToArray();
									item.itemID = 32;
									inventory.items = inventory.items.Append(item).ToArray();
									sendInventory(peer, inventory);
								}
								else if (str.Substring(0, 6) == "/team ")
								{
									int val = 0;
									val = Convert.ToInt32(str.Substring(6, cch.Length - 6 - 1));
									PlayerMoving data;
									//data.packetType = 0x14;
									data.packetType = 0x1B;
									//data.characterState = 0x924; // animation
									data.characterState = 0x0; // animation
									data.x = 0;
									data.y = 0;
									data.punchX = val;
									data.punchY = 0;
									data.XSpeed = 0;
									data.YSpeed = 0;
									data.netID = (peer.Data as PlayerInfo).netID;
									data.plantingTree = 0;
									SendPacketRaw(4, packPlayerMoving(data), 56, 0, peer, 0);

								}
								else if (str.Substring(0, 7) == "/color ")
								{
									(peer.Data as PlayerInfo).skinColor =
										Convert.ToUInt32(str.Substring(6, cch.Length - 6 - 1));
									sendClothes(peer);
								}

								if (str.Substring(0, 4) == "/who")
								{
									sendWho(peer);
								}

								if (str.Length != 0 && str[0] == '/')
								{
									sendAction(peer, (peer.Data as PlayerInfo).netID, str);
								}
								else if (str.Length > 0)
								{
									sendChatMessage(peer, (peer.Data as PlayerInfo).netID, str);
								}

							}
							if (!(peer.Data as PlayerInfo).isIn)
							{
								GamePacket p = packetEnd(appendString(
									appendString(
										appendString(
											appendString(
												appendInt(
													appendString(createPacket(),
//														"OnSuperMainStartAcceptLogonHrdxs47254722215a"), -703607114),
												"OnSuperMainStartAcceptLogonHrdxs47254722215a"), -1054420378),
												"cdn.growtopiagame.com"), "cache/"),
										"cc.cz.madkite.freedom org.aqua.gg idv.aqua.bulldog com.cih.gamecih2 com.cih.gamecih com.cih.game_cih cn.maocai.gamekiller com.gmd.speedtime org.dax.attack com.x0.strai.frep com.x0.strai.free org.cheatengine.cegui org.sbtools.gamehack com.skgames.traffikrider org.sbtoods.gamehaca com.skype.ralder org.cheatengine.cegui.xx.multi1458919170111 com.prohiro.macro me.autotouch.autotouch com.cygery.repetitouch.free com.cygery.repetitouch.pro com.proziro.zacro com.slash.gamebuster"),
									"proto=42|choosemusic=audio/mp3/about_theme.mp3|active_holiday=0|"));
								//for (int i = 0; i < p.len; i++) cout << (int)*(p.data + i) << " ";
								peer.Send(p.data, 0, ENetPacketFlags.Reliable);

								//enet_host_flush(server);
								string[] str = Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray()).Split("\n".ToCharArray());
								foreach (string to in str)
								{
									if (to == "") continue;
									string id = to.Substring(0, to.IndexOf("|"));
									string act = to.Substring(to.IndexOf("|") + 1, to.Length - to.IndexOf("|") - 1);
									if (id == "tankIDName")
									{
										(peer.Data as PlayerInfo).tankIDName = act;
										(peer.Data as PlayerInfo).haveGrowId = true;
									}
									else if (id == "tankIDPass")
									{
										(peer.Data as PlayerInfo).tankIDPass = act;
									}
									else if (id == "requestedName")
									{
										(peer.Data as PlayerInfo).requestedName = act;
									}
									else if (id == "country")
									{
										(peer.Data as PlayerInfo).country = act;
									}
								}

								if (!(peer.Data as PlayerInfo).haveGrowId)
								{
									(peer.Data as PlayerInfo).rawName = "";
									(peer.Data as PlayerInfo).displayName = "Fake " + PlayerDB.fixColors((peer.Data as PlayerInfo).
										requestedName.Substring(0, (peer.Data as PlayerInfo).
										requestedName.Length > 15 ? 15 : (peer.Data as PlayerInfo).
										requestedName.Length));
								}
								else
								{
									(peer.Data as PlayerInfo).rawName =
										PlayerDB.getProperName((peer.Data as PlayerInfo).tankIDName);
									int logStatus = PlayerDB.playerLogin(peer, (peer.Data as PlayerInfo).rawName,
										(peer.Data as PlayerInfo).tankIDPass);
									if (logStatus == 1)
									{
										GamePacket p1 = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`rYou have successfully logged into your account!``"));
										peer.Send(p1.data, 0, ENetPacketFlags.Reliable);
										(peer.Data as PlayerInfo).displayName = (peer.Data as PlayerInfo).tankIDName;
									}
									else
									{
										GamePacket p1 = packetEnd(appendString(
											appendString(createPacket(), "OnConsoleMessage"),
											"`rWrong username or password!``"));

										peer.Send(p1.data, 0, ENetPacketFlags.Reliable);
										peer.DisconnectLater(0);
										return;
									}
								}

								foreach (char c in (peer.Data as PlayerInfo).displayName)
									if (c < 0x20 || c > 0x7A) (peer.Data as PlayerInfo).displayName = "Bad characters in name, remove them!";

								if ((peer.Data as PlayerInfo).country.Length > 4)
								{
									(peer.Data as PlayerInfo).country = "us";
								}
								if (getAdminLevel((peer.Data as PlayerInfo).rawName, (peer.Data as PlayerInfo).tankIDPass) > 0)
								{
									(peer.Data as PlayerInfo).country = "../cash_icon_overlay";
								}

								GamePacket p2 = packetEnd(appendString(appendString(
									appendInt(appendString(createPacket(), "SetHasGrowID"),
										((peer.Data as PlayerInfo).haveGrowId ? 1 : 0))
									, (peer.Data as PlayerInfo).tankIDName)
									, (peer.Data as PlayerInfo).tankIDPass));

								peer.Send(p2.data, 0, ENetPacketFlags.Reliable);
							}

							string pStr = Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray());
							//if (strcmp(GetTextPointerFromPacket(event.packet), "action|enter_game\n") == 0 && !((PlayerInfo*)(event.peer->data))->isIn)
							if (pStr.Contains("action|enter_game") && !(peer.Data as PlayerInfo).isIn)
							{
								Console.WriteLine("And we are in!");
								(peer.Data as PlayerInfo).isIn = true;
								sendWorldOffers(peer);
								GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnConsoleMessage"),
									"C# Server made by willi123yao."));

								peer.Send(p.data, 0, ENetPacketFlags.Reliable);

								//enet_host_flush(server);
								PlayerInventory inventory = new PlayerInventory();
								for (int i = 0; i < 200; i++)
								{
									InventoryItem it = new InventoryItem();
									it.itemID = (short) ((i * 2) + 2);
									it.itemCount = 200;
									inventory.items = inventory.items.Append(it).ToArray();
								}

								(peer.Data as PlayerInfo).inventory = inventory;

								{
									//GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnDialogRequest"), "set_default_color|`o\n\nadd_label_with_icon|big|`wThe Growtopia Gazette``|left|5016|\n\nadd_spacer|small|\n\nadd_image_button|banner|interface/large/news_banner.rttex|noflags|||\n\nadd_spacer|small|\n\nadd_textbox|`wSeptember 10:`` `5Surgery Stars end!``|left|\n\nadd_spacer|small|\n\n\n\nadd_textbox|Hello Growtopians,|left|\n\nadd_spacer|small|\n\n\n\nadd_textbox|Surgery Stars is over! We hope you enjoyed it and claimed all your well-earned Summer Tokens!|left|\n\nadd_spacer|small|\n\nadd_spacer|small|\n\nadd_textbox|As we announced earlier, this month we are releasing the feature update a bit later, as we're working on something really cool for the monthly update and we're convinced that the wait will be worth it!|left|\n\nadd_spacer|small|\n\nadd_textbox|Check the Forum here for more information!|left|\n\nadd_spacer|small|\n\nadd_url_button|comment|`wSeptember Updates Delay``|noflags|https://www.growtopiagame.com/forums/showthread.php?510657-September-Update-Delay&p=3747656|Open September Update Delay Announcement?|0|0|\n\nadd_spacer|small|\n\nadd_spacer|small|\n\nadd_textbox|Also, we're glad to invite you to take part in our official Growtopia survey!|left|\n\nadd_spacer|small|\n\nadd_url_button|comment|`wTake Survey!``|noflags|https://ubisoft.ca1.qualtrics.com/jfe/form/SV_1UrCEhjMO7TKXpr?GID=26674|Open the browser to take the survey?|0|0|\n\nadd_spacer|small|\n\nadd_textbox|Click on the button above and complete the survey to contribute your opinion to the game and make Growtopia even better! Thanks in advance for taking the time, we're looking forward to reading your feedback!|left|\n\nadd_spacer|small|\n\nadd_spacer|small|\n\nadd_textbox|And for those who missed PAW, we made a special video sneak peek from the latest PAW fashion show, check it out on our official YouTube channel! Yay!|left|\n\nadd_spacer|small|\n\nadd_url_button|comment|`wPAW 2018 Fashion Show``|noflags|https://www.youtube.com/watch?v=5i0IcqwD3MI&feature=youtu.be|Open the Growtopia YouTube channel for videos and tutorials?|0|0|\n\nadd_spacer|small|\n\nadd_textbox|Lastly, check out other September updates:|left|\n\nadd_spacer|small|\n\nadd_label_with_icon|small|IOTM: The Sorcerer's Tunic of Mystery|left|24|\n\nadd_label_with_icon|small|New Legendary Summer Clash Branch|left|24|\n\nadd_spacer|small|\n\nadd_textbox|`$- The Growtopia Team``|left|\n\nadd_spacer|small|\n\nadd_spacer|small|\n\n\n\n\n\nadd_url_button|comment|`wOfficial YouTube Channel``|noflags|https://www.youtube.com/c/GrowtopiaOfficial|Open the Growtopia YouTube channel for videos and tutorials?|0|0|\n\nadd_url_button|comment|`wSeptember's IOTM: `8Sorcerer's Tunic of Mystery!````|noflags|https://www.growtopiagame.com/forums/showthread.php?450065-Item-of-the-Month&p=3392991&viewfull=1#post3392991|Open the Growtopia website to see item of the month info?|0|0|\n\nadd_spacer|small|\n\nadd_label_with_icon|small|`4WARNING:`` `5Drop games/trust tests`` and betting games (like `5Casinos``) are not allowed and will result in a ban!|left|24|\n\nadd_label_with_icon|small|`4WARNING:`` Using any kind of `5hacked client``, `5spamming/text pasting``, or `5bots`` (even with an alt) will likely result in losing `5ALL`` your accounts. Seriously.|left|24|\n\nadd_label_with_icon|small|`4WARNING:`` `5NEVER enter your GT password on a website (fake moderator apps, free gemz, etc) - it doesn't work and you'll lose all your stuff!|left|24|\n\nadd_spacer|small|\n\nadd_url_button|comment|`wGrowtopia on Facebook``|noflags|http://growtopiagame.com/facebook|Open the Growtopia Facebook page in your browser?|0|0|\n\nadd_spacer|small|\n\nadd_button|rules|`wHelp - Rules - Privacy Policy``|noflags|0|0|\n\n\nadd_quick_exit|\n\nadd_spacer|small|\nadd_url_button|comment|`wVisit Growtopia Forums``|noflags|http://www.growtopiagame.com/forums|Visit the Growtopia forums?|0|0|\nadd_spacer|small|\nadd_url_button||`wWOTD: `1THELOSTGOLD`` by `#iWasToD````|NOFLAGS|OPENWORLD|THELOSTGOLD|0|0|\nadd_spacer|small|\nadd_url_button||`wVOTW: `1Yodeling Kid - Growtopia Animation``|NOFLAGS|https://www.youtube.com/watch?v=UMoGmnFvc58|Watch 'Yodeling Kid - Growtopia Animation' by HyerS on YouTube?|0|0|\nend_dialog|gazette||OK|"));
									GamePacket p4 = packetEnd(appendString(
										appendString(createPacket(), "OnDialogRequest"),
										"set_default_color|`o\n\nadd_label_with_icon|big|`wThe Growtopia Gazette``|left|5016|\n\nadd_spacer|small|\nadd_label_with_icon|small|`4WARNING:`` `5Worlds (and accounts)`` might be deleted at any time if database issues appear (once per day or week).|left|4|\nadd_label_with_icon|small|`4WARNING:`` `5Accounts`` are in beta, bugs may appear and they will be probably deleted often, because of new account updates, which will cause database incompatibility.|left|4|\nadd_spacer|small|\n\nadd_url_button||``Watch: `1Watch a video about GT Private Server``|NOFLAGS|https://www.youtube.com/watch?v=_3avlDDYBBY|Open link?|0|0|\nadd_url_button||``Channel: `1Watch Growtopia Noobs' channel``|NOFLAGS|https://www.youtube.com/channel/UCLXtuoBlrXFDRtFU8vPy35g|Open link?|0|0|\nadd_url_button||``Items: `1Item database by Nenkai``|NOFLAGS|https://raw.githubusercontent.com/Nenkai/GrowtopiaItemDatabase/master/GrowtopiaItemDatabase/CoreData.txt|Open link?|0|0|\nadd_url_button||``Discord: `1GT Private Server Discord``|NOFLAGS|https://discord.gg/8WUTs4v|Open the link?|0|0|\nadd_quick_exit|\nadd_button|chc0|Close|noflags|0|0|\nnend_dialog|gazette||OK|"));
									peer.Send(p4.data, 0, ENetPacketFlags.Reliable);

									//enet_host_flush(server);
								}
							}
							if (Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray()) == "action|refresh_item_data\n")
							{
								if (itemsDat != null)
								{
									peer.Send(itemsDat, 0, ENetPacketFlags.Reliable);
									(peer.Data as PlayerInfo).isUpdating = true;
									peer.DisconnectLater(0);
									//enet_host_flush(server);
								}

								// TODO FIX refresh_item_data ^^^^^^^^^^^^^^
							}
							break;
						}
						default:
							Console.WriteLine("Unknown packet type "+messageType);
							break;
						case 3:
						{
							//cout << GetTextPointerFromPacket(event.packet) << endl;
							bool isJoinReq = false;
							foreach(string to in Encoding.ASCII.GetString(pak.Take(pak.Length - 1).Skip(4).ToArray()).Split("\n".ToCharArray()))
							{
								if (to == "") continue;
								string id = to.Substring(0, to.IndexOf("|"));
								string act = to.Substring(to.IndexOf("|") + 1, to.Length - to.IndexOf("|") - 1);
								if (id == "name" && isJoinReq)
								{
									Console.WriteLine("Entering some world...");
									try
									{
										WorldInfo info = worldDB.get(act);
										sendWorld(peer, info);

										int x = 3040;
										int y = 736;

										for (int j = 0; j < info.width * info.height; j++)
										{
											if (info.items[j].foreground == 6)
											{
												x = (j % info.width) * 32;
												y = (j / info.width) * 32;
											}
										}

										GamePacket p = packetEnd(appendString(appendString(createPacket(), "OnSpawn"),
											"spawn|avatar\nnetID|" + cId + "\nuserID|" +
											cId + "\ncolrect|0|0|20|30\nposXY|" + x + "|" + y + "\nname|``" 
											+ (peer.Data as PlayerInfo).displayName + "``\ncountry|" 
											+ (peer.Data as PlayerInfo).country 
											+ "\ninvis|0\nmstate|0\nsmstate|0\ntype|local\n"));
										//for (int i = 0; i < p.len; i++) cout << (int)*(p.data + i) << " ";

										peer.Send(p.data, 0, ENetPacketFlags.Reliable);
										//enet_host_flush(server);
										(peer.Data as PlayerInfo).netID = cId;
										onPeerConnect(peer);
										cId++;

										sendInventory(peer, (peer.Data as PlayerInfo).inventory);

									}
									catch 
									{
										int e = 0;
										if (e == 1)
										{
											(peer.Data as PlayerInfo).currentWorld = "EXIT";
											GamePacket p = packetEnd(appendString(
												appendString(createPacket(), "OnConsoleMessage"),
												"You have exited the world."));

											peer.Send(p.data, 0, ENetPacketFlags.Reliable);
											//enet_host_flush(server);
										}
										else if (e == 2)
										{
											(peer.Data as PlayerInfo).currentWorld = "EXIT";
											GamePacket p = packetEnd(appendString(
												appendString(createPacket(), "OnConsoleMessage"),
												"You have entered bad characters in the world name!"));

											peer.Send(p.data, 0, ENetPacketFlags.Reliable);
											//enet_host_flush(server);
										}
										else if (e == 3)
										{
											(peer.Data as PlayerInfo).currentWorld = "EXIT";
											GamePacket p = packetEnd(appendString(
												appendString(createPacket(), "OnConsoleMessage"),
												"Exit from what? Click back if you're done playing."));
											
											peer.Send(p.data, 0, ENetPacketFlags.Reliable);
											//enet_host_flush(server);
										}
										else
										{
											(peer.Data as PlayerInfo).currentWorld = "EXIT";
											GamePacket p = packetEnd(appendString(
												appendString(createPacket(), "OnConsoleMessage"),
												"I know this menu is magical and all, but it has its limitations! You can't visit this world!"));

											peer.Send(p.data, 0, ENetPacketFlags.Reliable);
											//enet_host_flush(server);
										}
									}
								}

								if (id == "action")
								{

									if (act == "join_request")
									{
										isJoinReq = true;
									}

									if (act == "quit_to_exit")
									{
										sendPlayerLeave(peer, peer.Data as PlayerInfo);
										(peer.Data as PlayerInfo).currentWorld = "EXIT";
										sendWorldOffers(peer);

									}

									if (act == "quit")
									{
										peer.DisconnectLater(0);
									}
								}
							}

							break;
						}
						case 4:
						{
							{
								byte[] tankUpdatePacket = pak.Skip(4).ToArray();

								if (tankUpdatePacket.Length != 0)
								{
									PlayerMoving pMov = unpackPlayerMoving(tankUpdatePacket);
									switch (pMov.packetType)
									{
										case 0:
											(peer.Data as PlayerInfo).x = (int) pMov.x;
											(peer.Data as PlayerInfo).y = (int) pMov.y;
											(peer.Data as PlayerInfo).isRotatedLeft = (pMov.characterState & 0x10) != 0 ;
											sendPData(peer, pMov);
											if (!(peer.Data as PlayerInfo).joinClothesUpdated)
											{
												(peer.Data as PlayerInfo).joinClothesUpdated = true;
												updateAllClothes(peer);
											}

											break;

										default:
											break;
									}

									PlayerMoving data2 = unpackPlayerMoving(tankUpdatePacket);
									//cout << data2->packetType << endl;
									if (data2.packetType == 11)
									{
										//cout << pMov->x << ";" << pMov->y << ";" << pMov->plantingTree << ";" << pMov->punchX << endl;
										//sendDrop(((PlayerInfo*)(event.peer->data))->netID, ((PlayerInfo*)(event.peer->data))->x, ((PlayerInfo*)(event.peer->data))->y, pMov->punchX, 1, 0);
										// lets take item
									}

									if (data2.packetType == 7)
									{
										//cout << pMov->x << ";" << pMov->y << ";" << pMov->plantingTree << ";" << pMov->punchX << endl;
										sendWorldOffers(peer);
										// lets take item
									}

									if (data2.packetType == 10)
									{
										//cout << pMov->x << ";" << pMov->y << ";" << pMov->plantingTree << ";" << pMov->punchX << ";" << pMov->punchY << ";" << pMov->characterState << endl;
										ItemDefinition def = new ItemDefinition();
										try
										{
											def = getItemDef(pMov.plantingTree);
										}
										catch
										{
											
										}

										switch (def.clothType)
										{
											case ClothTypes.HAIR:
											{
												if ((peer.Data as PlayerInfo).cloth_hair == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_hair = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_hair = pMov.plantingTree;
												break;
											}

											case ClothTypes.SHIRT:
											{
												if ((peer.Data as PlayerInfo).cloth_shirt == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_shirt = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_shirt = pMov.plantingTree;
												break;	
											}

											case ClothTypes.PANTS:
											{
												if ((peer.Data as PlayerInfo).cloth_pants == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_pants = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_pants = pMov.plantingTree;
												break;
											}

											case ClothTypes.FEET:
											{
												if ((peer.Data as PlayerInfo).cloth_feet == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_feet = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_feet = pMov.plantingTree;
												break;
											}
												
											case ClothTypes.FACE:
											{
												if ((peer.Data as PlayerInfo).cloth_face == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_face = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_face = pMov.plantingTree;
												break;
											}

											case ClothTypes.HAND:
											{
												if ((peer.Data as PlayerInfo).cloth_hand == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_hand = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_hand = pMov.plantingTree;
												break;
											}
												
											case ClothTypes.BACK:
											{
												if ((peer.Data as PlayerInfo).cloth_back == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_back = 0;
													(peer.Data as PlayerInfo).canDoubleJump = false;
													sendState(peer);
													break;
												}
												{
													(peer.Data as PlayerInfo).cloth_back = pMov.plantingTree;
													int item = pMov.plantingTree;
													if (item == 156 || item == 362 || item == 678 || item == 736 ||
														item == 818 || item == 1206 || item == 1460 || item == 1550 ||
														item == 1574 || item == 1668 || item == 1672 || item == 1674 ||
														item == 1784 || item == 1824 || item == 1936 || item == 1938 ||
														item == 1970 || item == 2254 || item == 2256 || item == 2258 ||
														item == 2260 || item == 2262 || item == 2264 || item == 2390 ||
														item == 2392 || item == 3120 || item == 3308 || item == 3512 ||
														item == 4534 || item == 4986 || item == 5754 || item == 6144 ||
														item == 6334 || item == 6694 || item == 6818 || item == 6842 ||
														item == 1934 || item == 3134 || item == 6004 || item == 1780 ||
														item == 2158 || item == 2160 || item == 2162 || item == 2164 ||
														item == 2166 || item == 2168 || item == 2438 || item == 2538 ||
														item == 2778 || item == 3858 || item == 350 || item == 998 ||
														item == 1738 || item == 2642 || item == 2982 || item == 3104 ||
														item == 3144 || item == 5738 || item == 3112 || item == 2722 ||
														item == 3114 || item == 4970 || item == 4972 || item == 5020 ||
														item == 6284 || item == 4184 || item == 4628 || item == 5322 ||
														item == 4112 || item == 4114 || item == 3442)
													{
														(peer.Data as PlayerInfo).canDoubleJump = true;
													}
													else
													{
														(peer.Data as PlayerInfo).canDoubleJump = false;
													}

													// ^^^^ wings
													sendState(peer);
												}
												break;
											}
											
											case ClothTypes.MASK:
											{
												if ((peer.Data as PlayerInfo).cloth_mask == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_mask = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_mask = pMov.plantingTree;
												break;
											}

											case ClothTypes.NECKLACE:
											{
												if ((peer.Data as PlayerInfo).cloth_necklace == pMov.plantingTree)
												{
													(peer.Data as PlayerInfo).cloth_necklace = 0;
													break;
												}
												(peer.Data as PlayerInfo).cloth_necklace = pMov.plantingTree;
												break;
											}

											default:
											{
												Console.WriteLine("Invalid item activated: "+pMov.plantingTree+" by "
												                  +(peer.Data as PlayerInfo).displayName);
												break;
											}		
										}

										sendClothes(peer);
										// activate item
									}

									if (data2.packetType == 18)
									{
										sendPData(peer, pMov);
										// add talk buble
									}

									if (data2.punchX != -1 && data2.punchY != -1)
									{
										//cout << data2->packetType << endl;
										if (data2.packetType == 3)
										{
											sendTileUpdate(data2.punchX, data2.punchY, data2.plantingTree,
												(peer.Data as PlayerInfo).netID, peer);
										}

									}

								}
								else
								{
									Console.WriteLine("Got bad tank packet");
								}
							}
						}
							break;
						case 5:
							break;
						case 6:
							//cout << GetTextPointerFromPacket(event.packet) << endl;
							break;
					}
				};
				eve.Peer.OnDisconnect += (object send, uint ev) =>
				{
					ENetPeer peer = send as ENetPeer;
					Console.WriteLine("Peer disconnected");
					sendPlayerLeave(peer, peer.Data as PlayerInfo);
					(peer.Data as PlayerInfo).inventory.items = new InventoryItem[] {};
					peer.Data = null;
					peers.Remove(peer);
				};
			};
			server.StartServiceThread();
			Thread.Sleep(Timeout.Infinite);
			Console.WriteLine("Program ended??? Huh?");
		}
	}
}