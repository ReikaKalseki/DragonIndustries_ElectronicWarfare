using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace DragonIndustries {
	
    public static class Sync {
		
        private static bool initialized = false;
        private const ushort packetChannel = 30608;
        
        internal enum Packets {
        	NONE,
        	EMP,
        	HACKING,
        	CLOAKING,
        };
        
        public static bool HasBeenInitialized {
            get {
                return initialized;
            }
        }

        public static void initialize() {
            if (MyAPIGateway.Session.Player != null && !initialized) {
                IO.log("Registering packet channels");
                MyAPIGateway.Multiplayer.RegisterMessageHandler(packetChannel, handleGenericPacket);
            }
            initialized = true;
        }

        public static void unload() {
            initialized = false;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(packetChannel, handleGenericPacket);
        }
        
        private static void handleGenericPacket(byte[] data) {
        	Packets type = (Packets)BitConverter.ToInt32(data, 0);
        	try {
        		Packet p = new Packet(type, data);
        		p.readInt(); //pop off the type
        		
        		long id = p.readLong();
                IMyEntity entity;
                if (MyAPIGateway.Entities.TryGetEntityById(id, out entity)) {
		        	switch(type) {
		        		case Packets.EMP:
		        			handleEMPPacket(p, entity);
		        			break;
		        		case Packets.HACKING:
		        			handleHackingPacket(p, entity);
		        			break;
		        		case Packets.CLOAKING:
		        			handleCloakingPacket(p, entity);
		        			break;
		        	}
                	//IO.log("Successfully handled packet "+type.ToString());
                }
                else {
                	IO.log("Target entity (ID="+id+") did not exist on client to receive sync packet type "+type.ToString()+"!");
                }
        	}
            catch (Exception e) {
        		IO.log("Threw exception while handling packet type "+type.ToString()+": "+e.ToString());
            }
        }
        
        private static void handleEMPPacket(Packet p, IMyEntity entity) {
            try {
                EMP logic = entity.GameLogic.GetAs<EMP>();
                if (logic != null) {
                	logic.currentEmissive = p.readInt();
                	logic.cyclesOn = p.readInt();
                	logic.cyclesUntilFire = p.readInt();
                	logic.readyToFire = p.readBoolean();
                }
                else {
                   IO.log("EMP logic did not exist on client to process sync packet!");
                }
            }
            catch (Exception e) {
        		IO.log("Threw exception while handling EMP packet: "+e.ToString());
            }
        }

        private static void handleHackingPacket(Packet p, IMyEntity entity) {
            try {
                HackingBlock logic = entity.GameLogic.GetAs<HackingBlock>();
                if (logic != null) {
                    logic.state = (HackingBlock.States)p.readInt();
                    logic.targetID = p.readLong();
                    logic.targetDifficulty = p.readFloat();
                    logic.targetTime = p.readInt();
                    logic.cyclesUntilAttempt = p.readInt();
                    logic.isIdle = p.readBoolean();
                    logic.updateRender();
                }
                else {
                   IO.log("Hacking computer logic did not exist on client to process sync packet!");
                }
            }
            catch (Exception e) {
        		IO.log("Threw exception while handling hacking packet: "+e.ToString());
            }
        }
        
        private static void handleCloakingPacket(Packet p, IMyEntity entity) {
        	try {
                CloakingDevice logic = entity.GameLogic.GetAs<CloakingDevice>();
                if (logic != null) {
                	logic.enableDerendering = p.readBoolean();
                }
                else {
                   IO.log("Cloaking device logic did not exist on client to process sync packet!");
                }
            }
            catch (Exception e) {
        		IO.log("Threw exception while handling cloaking packet: "+e.ToString());
            }
        }
        
        public static void sendSyncData(LogicCore b) {
        	Packets type = Packets.NONE;
        	if (b is EMP) {
        		type = Packets.EMP;
        	}
        	else if (b is HackingBlock) {
        		type = Packets.HACKING;
        	}
        	else if (b is CloakingDevice) { //b is CloakingDevice
        		type = Packets.CLOAKING;
        	}
        	
        	if (type != Packets.NONE) {
	        	Packet p = new Packet(type);
        		p.writeInt((int)type);
	        	p.writeLong(b.Entity.EntityId);
	        	switch(type) {
	        		case Packets.EMP:
	        			sendEMPData(p, b as EMP);
	        		break;
	        		case Packets.HACKING:
	        			sendHackData(p, b as HackingBlock);
	        		break;
	        		case Packets.CLOAKING:
	        			sendCloakData(p, b as CloakingDevice);
	        		break;
	        	}
	        	p.sendToAll();
        		//IO.log("Send a packet to sync "+b+" with ID "+b.Entity.EntityId);
        	}
        	else {
        		IO.log("Tried to send a packet to sync an unhandled type "+b+"!!");
        	}
        }

        private static void sendEMPData(Packet p, EMP b) {
            try {        	
                p.writeInt(b.currentEmissive);
                p.writeInt(b.cyclesOn);
                p.writeInt(b.cyclesUntilFire);
                p.writeBoolean(b.readyToFire);        		
            }
            catch (Exception e) {
                IO.log("Threw exception while dispatching EMP packet: "+e.ToString());
            }
        }

        private static void sendHackData(Packet p, HackingBlock b) {
            try {
        		p.writeInt((int)b.state);
        		p.writeLong(b.targetID);
        		p.writeFloat(b.targetDifficulty);
        		p.writeInt(b.targetTime);
        		p.writeInt(b.cyclesUntilAttempt);
        		p.writeBoolean(b.isIdle);       
            }
            catch (Exception e) {
                IO.log("Threw exception while dispatching hacking packet: "+e.ToString());
            }
        }

        private static void sendCloakData(Packet p, CloakingDevice b) {
            try {
        		p.writeBoolean(b.enableDerendering);
            }
            catch (Exception e) {
                IO.log("Threw exception while dispatching cloaking packet: "+e.ToString());
            }
        }
        
        internal class Packet {
        	
        	private readonly Packets ID;
        	private readonly List<byte> data = new List<byte>();
        	
        	private int readIndex;
        	
        	internal Packet(Packets p) {
        		ID = p;
        	}
        	
        	internal Packet(Packets p, byte[] rawdata) : this(p) {
        		data.AddArray(rawdata);
        	}
        	
        	public void sendToAll() {
        		List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Multiplayer.Players.GetPlayers(players);
                foreach (IMyPlayer ep in players) {
                	sendToPlayer(ep);
                }
        	}
        	
        	public void sendToPlayer(IMyPlayer ep) {
        		MyAPIGateway.Multiplayer.SendMessageTo(packetChannel, toSerial(), ep.SteamUserId);
        	}
        	
        	private List<byte> read(int bytes, bool remove = false) {
        		List<byte> li = data.GetRange(readIndex, bytes);
        		
        		if (remove)
        			li.RemoveRange(readIndex, bytes);
        		else
        			readIndex += bytes;
        		
        		return li;
        	}
        	
        	//Read functions
        	public int readInt(bool remove = false) {
        		List<byte> li = read(4, remove);
        		return BitConverter.ToInt32(li.ToArray(), 0);
        	}
        	
        	public bool readBoolean(bool remove = false) {
        		List<byte> li = read(1, remove);
        		return BitConverter.ToBoolean(li.ToArray(), 0);
        	}
        	
        	public short readShort(bool remove = false) {
        		List<byte> li = read(2, remove);
        		return BitConverter.ToInt16(li.ToArray(), 0);
        	}
        	
			public long readLong(bool remove = false) {
        		List<byte> li = read(8, remove);
        		return BitConverter.ToInt64(li.ToArray(), 0);
        	}
        	
        	public float readFloat(bool remove = false) {
        		List<byte> li = read(4, remove);
        		return BitConverter.ToSingle(li.ToArray(), 0);
        	}
        	
        	public char readChar(bool remove = false) {
        		List<byte> li = read(2, remove);
        		return BitConverter.ToChar(li.ToArray(), 0);
        	}
        	
        	public string readString(bool remove = false) {
        		int len = readInt(remove);
        		StringBuilder sb = new StringBuilder();
        		for (int i = 0; i < len; i++) {
        			sb.Append(readChar(remove));
        		}
        		return sb.ToString();
        	}
        	
        	//Write functions
        	public void writeInt(int val) {
        		byte[] dat = BitConverter.GetBytes(val);
        		//data.Add((byte)dat.Length);
        		foreach (byte b in dat) {
        			data.Add(b);
        		}
        	}
        	
        	public void writeBoolean(bool val) {
        		byte[] dat = BitConverter.GetBytes(val);
        		//data.Add((byte)dat.Length);
        		foreach (byte b in dat) {
        			data.Add(b);
        		}
        	}
        	
        	public void writeShort(short val) {
        		byte[] dat = BitConverter.GetBytes(val);
        		//data.Add((byte)dat.Length);
        		foreach (byte b in dat) {
        			data.Add(b);
        		}
        	}
        	
        	public void writeLong(long val) {
        		byte[] dat = BitConverter.GetBytes(val);
        		//data.Add((byte)dat.Length);
        		foreach (byte b in dat) {
        			data.Add(b);
        		}
        	}
        	
        	public void writeFloat(float val) {
        		byte[] dat = BitConverter.GetBytes(val);
        		//data.Add((byte)dat.Length);
        		foreach (byte b in dat) {
        			data.Add(b);
        		}
        	}
        	
        	public void writeChar(char val) {
        		byte[] dat = BitConverter.GetBytes(val);
        		//data.Add((byte)dat.Length);
        		foreach (byte b in dat) {
        			data.Add(b);
        		}
        	}
        	
        	public void writeString(string val) {
        		writeInt(val.Length);
        		foreach (char c in val) {
        			writeChar(c);
        		}
        	}
        	
        	public byte[] toSerial() {
        		return data.ToArray();
        	}
        	
        }
    }
}