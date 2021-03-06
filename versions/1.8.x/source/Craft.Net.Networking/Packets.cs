﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication.ExtendedProtection.Configuration;
using System.Text;
using Craft.Net.Common;
using fNbt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Craft.Net.Networking
{
    public interface IPacket
    {
        /// <summary>
        /// Reads this packet data from the stream, not including its length or packet ID, and returns
        /// the new network state (if it has changed).
        /// </summary>
        NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction);
        /// <summary>
        /// Writes this packet data to the stream, not including its length or packet ID, and returns
        /// the new network state (if it has changed).
        /// </summary>
        NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction);
    }

    public struct UnknownPacket : IPacket
    {
        public long Id { get; set; }
        public byte[] Data { get; set; }

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            throw new NotImplementedException();
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Data.Length);
            stream.WriteUInt8Array(Data);
            return mode;
        }
    }

    #region Handshake

    public struct HandshakePacket : IPacket
    {
        public HandshakePacket(int protocolVersion, string hostname, ushort port, NetworkMode nextState)
        {
            ProtocolVersion = protocolVersion;
            ServerHostname = hostname;
            ServerPort = port;
            NextState = nextState;
        }

        public int ProtocolVersion;
        public string ServerHostname;
        public ushort ServerPort;
        public NetworkMode NextState;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            ProtocolVersion = stream.ReadVarInt();
            ServerHostname = stream.ReadString();
            ServerPort = stream.ReadUInt16();
            NextState = (NetworkMode)stream.ReadVarInt();
            return NextState;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(ProtocolVersion);
            stream.WriteString(ServerHostname);
            stream.WriteUInt16(ServerPort);
            stream.WriteVarInt((int)NextState);
            return NextState;
        }
    }

    #endregion

    #region Status

    public struct StatusRequestPacket : IPacket
    {

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            return mode;
        }
    }

    public struct StatusResponsePacket : IPacket
    {
        public StatusResponsePacket(ServerStatus status)
        {
            Status = status;
        }

        public ServerStatus Status;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Status = JsonConvert.DeserializeObject<ServerStatus>(stream.ReadString());
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(JsonConvert.SerializeObject(Status));
            return mode;
        }
    }

    public struct StatusPingPacket : IPacket
    {
        public StatusPingPacket(long time)
        {
            Time = time;
        }

        public long Time;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Time = stream.ReadInt64();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Time);
            return mode;
        }
    }

    #endregion

    #region Login

    public struct LoginDisconnectPacket : IPacket
    {
        public LoginDisconnectPacket(string jsonData)
        {
            JsonData = jsonData;
        }

        /// <summary>
        /// Note: This will eventually be replaced with a strongly-typed represenation of this data.
        /// </summary>
        public string JsonData;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            JsonData = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(JsonData);
            return mode;
        }
    }

    public struct EncryptionKeyRequestPacket : IPacket
    {
        public EncryptionKeyRequestPacket(string serverId, byte[] publicKey, byte[] verificationToken)
        {
            ServerId = serverId;
            PublicKey = publicKey;
            VerificationToken = verificationToken;
        }

        public string ServerId;
        public byte[] PublicKey;
        public byte[] VerificationToken;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            ServerId = stream.ReadString();
            var pkLength = stream.ReadInt16();
            PublicKey = stream.ReadUInt8Array(pkLength);
            var vtLength = stream.ReadInt16();
            VerificationToken = stream.ReadUInt8Array(vtLength);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(ServerId);
            stream.WriteInt16((short)PublicKey.Length);
            stream.WriteUInt8Array(PublicKey);
            stream.WriteInt16((short)VerificationToken.Length);
            stream.WriteUInt8Array(VerificationToken);
            return mode;
        }
    }

    public struct LoginSuccessPacket : IPacket
    {
        public LoginSuccessPacket(string uuid, string username)
        {
            UUID = uuid;
            Username = username;
        }

        public string UUID;
        public string Username;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            UUID = stream.ReadString();
            Username = stream.ReadString();
            return NetworkMode.Play;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(UUID);
            stream.WriteString(Username);
            return NetworkMode.Play;
        }
    }

    public struct LoginStartPacket : IPacket
    {
        public LoginStartPacket(string username)
        {
            Username = username;
        }

        public string Username;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Username = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Username);
            return mode;
        }
    }

    public struct EncryptionKeyResponsePacket : IPacket
    {
        public EncryptionKeyResponsePacket(byte[] sharedSecret, byte[] verificationToken)
        {
            SharedSecret = sharedSecret;
            VerificationToken = verificationToken;
        }

        public byte[] SharedSecret;
        public byte[] VerificationToken;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            var ssLength = stream.ReadInt16();
            SharedSecret = stream.ReadUInt8Array(ssLength);
            var vtLength = stream.ReadInt16();
            VerificationToken = stream.ReadUInt8Array(vtLength);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt16((short)SharedSecret.Length);
            stream.WriteUInt8Array(SharedSecret);
            stream.WriteInt16((short)VerificationToken.Length);
            stream.WriteUInt8Array(VerificationToken);
            return mode;
        }
    }

    #endregion

    #region Play

    public struct KeepAlivePacket : IPacket
    {
        public KeepAlivePacket(int keepAlive)
        {
            KeepAlive = keepAlive;
        }

        public int KeepAlive;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            KeepAlive = stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(KeepAlive);
            return mode;
        }
    }

    public struct JoinGamePacket : IPacket
    {
        public JoinGamePacket(int entityId, GameMode gameMode, Dimension dimension,
            Difficulty difficulty, byte maxPlayers, string levelType, bool reducedDebugInfo)
        {
            EntityId = entityId;
            GameMode = gameMode;
            Dimension = dimension;
            Difficulty = difficulty;
            MaxPlayers = maxPlayers;
            LevelType = levelType;
            ReducedDebugInfo = reducedDebugInfo;
        }

        public int EntityId;
        public GameMode GameMode;
        public Dimension Dimension;
        public Difficulty Difficulty;
        public byte MaxPlayers;
        public string LevelType;
        public bool ReducedDebugInfo;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadInt32();
            GameMode = (GameMode)stream.ReadUInt8();
            Dimension = (Dimension)stream.ReadInt8();
            Difficulty = (Difficulty)stream.ReadUInt8();
            MaxPlayers = stream.ReadUInt8();
            LevelType = stream.ReadString();
            ReducedDebugInfo = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(EntityId);
            stream.WriteUInt8((byte)GameMode);
            stream.WriteInt8((sbyte)Dimension);
            stream.WriteUInt8((byte)Difficulty);
            stream.WriteUInt8(MaxPlayers);
            stream.WriteString(LevelType);
            stream.WriteBoolean(ReducedDebugInfo);
            return mode;
        }
    }

    public struct ChatMessagePacket : IPacket
    {
        public ChatMessagePacket(string message, byte position)
        {
            Message = new ChatMessage(message);
            Position = position;
        }

        public ChatMessage Message;
        public byte Position;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Message = new ChatMessage(stream.ReadString());
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Message.RawMessage);
            return mode;
        }
    }

    public struct TimeUpdatePacket : IPacket
    {
        public TimeUpdatePacket(long worldAge, long timeOfDay)
        {
            WorldAge = worldAge;
            TimeOfDay = timeOfDay;
        }

        public long WorldAge;
        public long TimeOfDay;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WorldAge = stream.ReadInt64();
            TimeOfDay = stream.ReadInt64();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(WorldAge);
            stream.WriteInt64(TimeOfDay);
            return mode;
        }
    }

    public struct EntityEquipmentPacket : IPacket
    {
        public enum EntityEquipmentSlot
        {
            HeldItem = 0,
            Headgear = 1,
            Chestplate = 2,
            Pants = 3,
            Footwear = 4
        }

        public EntityEquipmentPacket(int entityId, EntityEquipmentSlot slot, ItemStack item)
        {
            EntityId = entityId;
            Slot = slot;
            Item = item;
        }

        public int EntityId;
        public EntityEquipmentSlot Slot;
        public ItemStack Item;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Slot = (EntityEquipmentSlot)stream.ReadInt16();
            Item = ItemStack.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt16((short)Slot);
            Item.WriteTo(stream);
            return mode;
        }
    }

    public struct SpawnPositionPacket : IPacket
    {
        public SpawnPositionPacket(Position position)
        {
            Pos = position;
        }

        public Position Pos;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            return mode;
        }
    }

    public struct UpdateHealthPacket : IPacket
    {
        public UpdateHealthPacket(float health, int food, float foodSaturation)
        {
            Health = health;
            Food = food;
            FoodSaturation = foodSaturation;
        }

        public float Health;
        public int Food;
        public float FoodSaturation;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Health = stream.ReadSingle();
            Food = stream.ReadVarInt();
            FoodSaturation = stream.ReadSingle();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteSingle(Health);
            stream.WriteVarInt(Food);
            stream.WriteSingle(FoodSaturation);
            return mode;
        }
    }

    public struct RespawnPacket : IPacket
    {
        public RespawnPacket(Dimension dimension, Difficulty difficulty, GameMode gameMode, string levelType)
        {
            Dimension = dimension;
            Difficulty = difficulty;
            GameMode = gameMode;
            LevelType = levelType;
        }

        public Dimension Dimension;
        public Difficulty Difficulty;
        public GameMode GameMode;
        public string LevelType;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Dimension = (Dimension)stream.ReadInt32();
            Difficulty = (Difficulty)stream.ReadUInt8();
            GameMode = (GameMode)stream.ReadUInt8();
            LevelType = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32((int)Dimension);
            stream.WriteUInt8((byte)Difficulty);
            stream.WriteUInt8((byte)GameMode);
            stream.WriteString(LevelType);
            return mode;
        }
    }

    public struct PlayerPacket : IPacket
    {
        public PlayerPacket(bool onGround)
        {
            OnGround = onGround;
        }

        public bool OnGround;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            OnGround = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteBoolean(OnGround);
            return mode;
        }
    }

    public struct PlayerPositionPacket : IPacket
    {
        public PlayerPositionPacket(double x, double y, double z, double stance, bool onGround)
        {
            X = x;
            Y = y;
            Z = z;
            Stance = stance;
            OnGround = onGround;
        }

        public double X, Y, Z;
        public double Stance;
        public bool OnGround;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            X = stream.ReadDouble();
            Stance = stream.ReadDouble();
            Y = stream.ReadDouble();
            Z = stream.ReadDouble();
            OnGround = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteDouble(X);
            stream.WriteDouble(Stance);
            stream.WriteDouble(Y);
            stream.WriteDouble(Z);
            stream.WriteBoolean(OnGround);
            return mode;
        }
    }

    public struct PlayerLookPacket : IPacket
    {
        public PlayerLookPacket(float yaw, float pitch, bool onGround)
        {
            Yaw = yaw;
            Pitch = pitch;
            OnGround = onGround;
        }

        public float Yaw, Pitch;
        public bool OnGround;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Yaw = stream.ReadSingle();
            Pitch = stream.ReadSingle();
            OnGround = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteSingle(Yaw);
            stream.WriteSingle(Pitch);
            stream.WriteBoolean(OnGround);
            return mode;
        }
    }

    public struct PlayerPositionAndLookPacket : IPacket
    {
        public PlayerPositionAndLookPacket(double x, double y, double z, float yaw, float pitch, byte flags)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            Flags = flags;
            Stance = null;
        }

        public PlayerPositionAndLookPacket(double x, double y, double z, double stance, float yaw, float pitch, byte flags)
            : this(x, y, z, yaw, pitch, flags)
        {
            Stance = stance;
        }

        //Y should be = to where the player's feet are
        public double X, Y, Z;
        public double? Stance;
        public float Yaw, Pitch;
        // <Dinnerbone> It's a bitfield, X/Y/Z/Y_ROT/X_ROT. If X is set, the x value is relative and not absolute.
        //X = 0x01 //Y = 0x02 //Z = 0x04 //Y_ROT = 0x08 //X_ROT = 0x10
        public byte Flags;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            X = stream.ReadDouble();
            if (direction == PacketDirection.Serverbound)
                Stance = stream.ReadDouble();
            Y = stream.ReadDouble();
            Z = stream.ReadDouble();
            Yaw = stream.ReadSingle();
            Pitch = stream.ReadSingle();
            Flags = (byte)stream.ReadByte();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteDouble(X);
            if (direction == PacketDirection.Serverbound)
                stream.WriteDouble(Stance.GetValueOrDefault());
            stream.WriteDouble(Y);
            stream.WriteDouble(Z);
            stream.WriteSingle(Yaw);
            stream.WriteSingle(Pitch);
            stream.WriteByte(Flags);
            return mode;
        }
    }

    public struct HeldItemPacket : IPacket
    {
        public HeldItemPacket(sbyte slot)
        {
            Slot = slot;
        }

        public short Slot;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            if (direction == PacketDirection.Clientbound)
                Slot = stream.ReadInt8();
            else
                Slot = stream.ReadInt16();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            if (direction == PacketDirection.Clientbound)
                stream.WriteInt8((sbyte)Slot);
            else
                stream.WriteInt16(Slot);
            return mode;
        }
    }

    public struct UseBedPacket : IPacket
    {
        public UseBedPacket(int entityId, Position position)
        {
            EntityId = entityId;
            Pos = position;
        }

        public int EntityId;
        public Position Pos;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Pos = new Position(stream.ReadInt64());
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt64(Pos.Encoded);
            return mode;
        }
    }

    public struct AnimationPacket : IPacket
    {
        public enum AnimationType
        {
            NoAnimation,
            SwingArm,
            Damage,
            LeaveBed,
            EatFood,
            CriticalEffect,
            MagicCriticalEffect,
            Crouch,
            Uncrouch
        }

        public AnimationPacket(byte filler)
        {
            EntityId = -1;
            Animation = AnimationType.NoAnimation;
        }

        public int EntityId;
        public AnimationType Animation;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            /*if (direction == PacketDirection.Clientbound)
                EntityId = stream.ReadVarInt();
            else
                EntityId = stream.ReadInt32();
            var animation = stream.ReadUInt8();
            if (direction == PacketDirection.Clientbound)
            {
                switch (animation)
                {
                    case 0: Animation = AnimationType.SwingArm; break;
                    case 1: Animation = AnimationType.Damage; break;
                    case 2: Animation = AnimationType.LeaveBed; break;
                    case 3: Animation = AnimationType.EatFood; break;
                    case 4: Animation = AnimationType.CriticalEffect; break;
                    case 5: Animation = AnimationType.MagicCriticalEffect; break;
                    case 104: Animation = AnimationType.Crouch; break;
                    case 105: Animation = AnimationType.Uncrouch; break;
                }
            }
            else
            {
                switch (animation)
                {
                    case 0: Animation = AnimationType.NoAnimation; break;
                    case 1: Animation = AnimationType.SwingArm; break;
                    case 2: Animation = AnimationType.Damage; break;
                    case 3: Animation = AnimationType.LeaveBed; break;
                    case 5: Animation = AnimationType.EatFood; break;
                    case 6: Animation = AnimationType.CriticalEffect; break;
                    case 7: Animation = AnimationType.MagicCriticalEffect; break;
                    case 104: Animation = AnimationType.Crouch; break;
                    case 105: Animation = AnimationType.Uncrouch; break;
                }
            }*/
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            //stream.WriteVarInt(EntityId);
            //stream.WriteUInt8((byte)Animation);
            return mode;
        }
    }

    public struct SpawnPlayerPacket : IPacket
    {
        public SpawnPlayerPacket(int entityId, string uuid, int x, int y, int z, byte yaw, byte pitch, short heldItem, MetadataDictionary metadata)
        {
            EntityId = entityId;
            UUID = uuid;

            JArray nameArray = JArray.Parse(new WebClient().DownloadString("https://api.mojang.com/user/profiles/" + uuid.Replace("-", "") + "/names"));
            JObject nameObject = JObject.Parse(nameArray.ToList()[nameArray.Count - 1].ToString());
            PlayerName = nameObject.GetValue("name").ToString();

            string texturesJson = new WebClient().DownloadString("https://sessionserver.mojang.com/session/minecraft/profile/" + uuid);
            JArray texturesArray = JArray.Parse(texturesJson);
            //DataCount = dataCount;
            DataName = texturesArray.ToList()[0].ToString();
            DataValue = texturesArray.ToList()[1].ToString();
            DataSignature = texturesArray.ToList()[2].ToString();

            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            HeldItem = heldItem;
            Metadata = metadata;
        }

        public int EntityId;
        public string PlayerName, UUID;
        // Data is the skin and cape, if they have one, of the player
        // No changes may need to be made if the Mojang API still accepts these requests
        //public int DataCount;
        public string DataName, DataValue, DataSignature;
        public int X, Y, Z;
        public byte Yaw, Pitch;
        public short HeldItem;
        public MetadataDictionary Metadata;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            UUID = stream.ReadString();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
            Z = stream.ReadInt32();
            Yaw = stream.ReadUInt8();
            Pitch = stream.ReadUInt8();
            HeldItem = stream.ReadInt16();
            Metadata = MetadataDictionary.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteString(UUID);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
            stream.WriteInt32(Z);
            stream.WriteUInt8(Yaw);
            stream.WriteUInt8(Pitch);
            stream.WriteInt16(HeldItem);
            Metadata.WriteTo(stream);
            return mode;
        }
    }

    public struct CollectItemPacket : IPacket
    {
        public CollectItemPacket(int itemId, int playerId)
        {
            ItemId = itemId;
            PlayerId = playerId;
        }

        public int ItemId;
        public int PlayerId;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            ItemId = stream.ReadVarInt();
            PlayerId = stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(ItemId);
            stream.WriteVarInt(PlayerId);
            return mode;
        }
    }

    public struct SpawnObjectPacket : IPacket
    {
        public SpawnObjectPacket(int entityId, byte type, int x,
             int y, int z, byte yaw, byte pitch,
             int data, short? speedX, short? speedY, short? speedZ)
        {
            EntityId = entityId;
            Type = type;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            Data = data;
            SpeedX = speedX;
            SpeedY = speedY;
            SpeedZ = speedZ;
        }

        public int EntityId;
        public byte Type;
        public int X, Y, Z;
        public byte Yaw, Pitch;
        public int Data;
        public short? SpeedX, SpeedY, SpeedZ;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Type = stream.ReadUInt8();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
            Z = stream.ReadInt32();
            Yaw = stream.ReadUInt8();
            Pitch = stream.ReadUInt8();
            Data = stream.ReadInt32();
            if (Data != 0)
            {
                SpeedX = stream.ReadInt16();
                SpeedY = stream.ReadInt16();
                SpeedZ = stream.ReadInt16();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(Type);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
            stream.WriteInt32(Z);
            stream.WriteUInt8(Yaw);
            stream.WriteUInt8(Pitch);
            stream.WriteInt32(Data);
            if (Data != 0)
            {
                stream.WriteInt16(SpeedX.Value);
                stream.WriteInt16(SpeedY.Value);
                stream.WriteInt16(SpeedZ.Value);
            }
            return mode;
        }
    }

    public struct SpawnMobPacket : IPacket
    {
        public SpawnMobPacket(int entityId, byte type, int x,
            int y, int z, byte yaw, byte pitch, byte headYaw, short velocityX,
            short velocityY, short velocityZ, MetadataDictionary metadata)
        {
            EntityId = entityId;
            Type = type;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
            HeadYaw = headYaw;
            VelocityX = velocityX;
            VelocityY = velocityY;
            VelocityZ = velocityZ;
            Metadata = metadata;
        }

        public int EntityId;
        public byte Type;
        public int X, Y, Z;
        public byte Yaw, Pitch, HeadYaw;
        public short VelocityX, VelocityY, VelocityZ;
        public MetadataDictionary Metadata;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Type = stream.ReadUInt8();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
            Z = stream.ReadInt32();
            Yaw = stream.ReadUInt8();
            Pitch = stream.ReadUInt8();
            HeadYaw = stream.ReadUInt8();
            VelocityX = stream.ReadInt16();
            VelocityY = stream.ReadInt16();
            VelocityZ = stream.ReadInt16();
            Metadata = MetadataDictionary.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(Type);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
            stream.WriteInt32(Z);
            stream.WriteUInt8(Yaw);
            stream.WriteUInt8(Pitch);
            stream.WriteUInt8(HeadYaw);
            stream.WriteInt16(VelocityX);
            stream.WriteInt16(VelocityY);
            stream.WriteInt16(VelocityZ);
            Metadata.WriteTo(stream);
            return mode;
        }
    }

    public struct SpawnPaintingPacket : IPacket
    {
        public SpawnPaintingPacket(int entityId, string title, Position position, byte direction)
        {
            EntityId = entityId;
            Title = title;
            Pos = position;
            Direction = direction;
        }

        public int EntityId;
        public string Title;
        public Position Pos;
        public byte Direction;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Title = stream.ReadString();
            Pos = new Position(stream.ReadInt64());
            Direction = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteString(Title);
            stream.WriteInt64(Pos.Encoded);
            stream.WriteUInt8(Direction);
            return mode;
        }
    }

    public struct SpawnExperienceOrbPacket : IPacket
    {
        public SpawnExperienceOrbPacket(int entityId, int x, int y,
            int z, short count)
        {
            EntityId = entityId;
            X = x;
            Y = y;
            Z = z;
            Count = count;
        }

        public int EntityId;
        public int X, Y, Z;
        public short Count;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
            Z = stream.ReadInt32();
            Count = stream.ReadInt16();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
            stream.WriteInt32(Z);
            stream.WriteInt16(Count);
            return mode;
        }
    }

    public struct EntityVelocityPacket : IPacket
    {
        public EntityVelocityPacket(int entityId, short velocityX, short velocityY,
            short velocityZ)
        {
            EntityId = entityId;
            VelocityX = velocityX;
            VelocityY = velocityY;
            VelocityZ = velocityZ;
        }

        public int EntityId;
        public short VelocityX, VelocityY, VelocityZ;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            VelocityX = stream.ReadInt16();
            VelocityY = stream.ReadInt16();
            VelocityZ = stream.ReadInt16();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt16(VelocityX);
            stream.WriteInt16(VelocityY);
            stream.WriteInt16(VelocityZ);
            return mode;
        }
    }

    public struct DestroyEntityPacket : IPacket
    {
        public DestroyEntityPacket(int[] entityIds)
        {
            EntityIds = entityIds;
        }

        public int[] EntityIds;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            var length = stream.ReadVarInt();
            EntityIds = stream.ReadInt32Array(length);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt((byte)EntityIds.Length);
            stream.WriteInt32Array(EntityIds);
            return mode;
        }
    }

    public struct EntityPacket : IPacket
    {
        public EntityPacket(int entityId)
        {
            EntityId = entityId;
        }

        public int EntityId;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadInt32();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(EntityId);
            return mode;
        }
    }

    public struct EntityRelativeMovePacket : IPacket
    {
        public EntityRelativeMovePacket(int entityId, sbyte deltaX, sbyte deltaY,
            sbyte deltaZ)
        {
            EntityId = entityId;
            DeltaX = deltaX;
            DeltaY = deltaY;
            DeltaZ = deltaZ;
        }

        public int EntityId;
        public sbyte DeltaX, DeltaY, DeltaZ;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            DeltaX = stream.ReadInt8();
            DeltaY = stream.ReadInt8();
            DeltaZ = stream.ReadInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt8(DeltaX);
            stream.WriteInt8(DeltaY);
            stream.WriteInt8(DeltaZ);
            return mode;
        }
    }

    public struct EntityLookPacket : IPacket
    {
        public EntityLookPacket(int entityId, byte yaw, byte pitch)
        {
            EntityId = entityId;
            Yaw = yaw;
            Pitch = pitch;
        }

        public int EntityId;
        public byte Yaw, Pitch;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Yaw = stream.ReadUInt8();
            Pitch = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(Yaw);
            stream.WriteUInt8(Pitch);
            return mode;
        }
    }

    public struct EntityLookAndRelativeMovePacket : IPacket
    {
        public EntityLookAndRelativeMovePacket(int entityId, sbyte deltaX, sbyte deltaY,
            sbyte deltaZ, byte yaw, byte pitch)
        {
            EntityId = entityId;
            DeltaX = deltaX;
            DeltaY = deltaY;
            DeltaZ = deltaZ;
            Yaw = yaw;
            Pitch = pitch;
        }

        public int EntityId;
        public sbyte DeltaX, DeltaY, DeltaZ;
        public byte Yaw, Pitch;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            DeltaX = stream.ReadInt8();
            DeltaY = stream.ReadInt8();
            DeltaZ = stream.ReadInt8();
            Yaw = stream.ReadUInt8();
            Pitch = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt8(DeltaX);
            stream.WriteInt8(DeltaY);
            stream.WriteInt8(DeltaZ);
            stream.WriteUInt8(Yaw);
            stream.WriteUInt8(Pitch);
            return mode;
        }
    }

    public struct EntityTeleportPacket : IPacket
    {
        public EntityTeleportPacket(int entityId, int x, int y,
            int z, byte yaw, byte pitch)
        {
            EntityId = entityId;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
        }

        public int EntityId;
        public int X, Y, Z;
        public byte Yaw, Pitch;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
            Z = stream.ReadInt32();
            Yaw = stream.ReadUInt8();
            Pitch = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
            stream.WriteInt32(Z);
            stream.WriteUInt8(Yaw);
            stream.WriteUInt8(Pitch);
            return mode;
        }
    }

    public struct EntityHeadLookPacket : IPacket
    {
        public EntityHeadLookPacket(int entityId, byte headYaw)
        {
            EntityId = entityId;
            HeadYaw = headYaw;
        }

        public int EntityId;
        public byte HeadYaw;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            HeadYaw = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(HeadYaw);
            return mode;
        }
    }

    public struct EntityStatusPacket : IPacket
    {
        public enum EntityStatus
        {
            Hurt = 2,
            Dead = 3,
            WolfTaming = 6,
            WolfTamed = 7,
            /// <summary>
            /// Shaking water off the wolf's body
            /// </summary>
            WolfShaking = 8,
            EatingAccepted = 9,
            /// <summary>
            /// Sheep eating grass
            /// </summary>
            SheepEating = 10
        }

        public EntityStatusPacket(int entityId, EntityStatus status)
        {
            EntityId = entityId;
            Status = status;
        }

        public int EntityId;
        public EntityStatus Status;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadInt32();
            Status = (EntityStatus)stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(EntityId);
            stream.WriteUInt8((byte)Status);
            return mode;
        }
    }

    public struct AttachEntityPacket : IPacket
    {
        public AttachEntityPacket(int entityId, int vehicleId, bool leash)
        {
            EntityId = entityId;
            VehicleId = vehicleId;
            Leash = leash;
        }

        public int EntityId, VehicleId;
        public bool Leash;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadInt32();
            VehicleId = stream.ReadInt32();
            Leash = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(EntityId);
            stream.WriteInt32(VehicleId);
            stream.WriteBoolean(Leash);
            return mode;
        }
    }

    public struct EntityMetadataPacket : IPacket
    {
        public EntityMetadataPacket(int entityId, MetadataDictionary metadata)
        {
            EntityId = entityId;
            Metadata = metadata;
        }

        public int EntityId;
        public MetadataDictionary Metadata;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Metadata = MetadataDictionary.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            Metadata.WriteTo(stream);
            return mode;
        }
    }

    public struct EntityEffectPacket : IPacket
    {
        public EntityEffectPacket(int entityId, byte effectId, byte amplifier, short duration, bool hideParticles)
        {
            EntityId = entityId;
            EffectId = effectId;
            Amplifier = amplifier;
            Duration = duration;
            HideParticles = hideParticles;
        }

        public int EntityId;
        public byte EffectId;
        public byte Amplifier;
        public short Duration;
        public bool HideParticles;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            EffectId = stream.ReadUInt8();
            Amplifier = stream.ReadUInt8();
            Duration = stream.ReadInt16();
            HideParticles = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(EffectId);
            stream.WriteUInt8(Amplifier);
            stream.WriteInt16(Duration);
            stream.WriteBoolean(HideParticles);
            return mode;
        }
    }

    public struct RemoveEntityEffectPacket : IPacket
    {
        public RemoveEntityEffectPacket(int entityId, byte effectId)
        {
            EntityId = entityId;
            EffectId = effectId;
        }

        public int EntityId;
        public byte EffectId;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            EffectId = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(EffectId);
            return mode;
        }
    }

    public struct SetExperiencePacket : IPacket
    {
        public SetExperiencePacket(float experienceBar, int level, int totalExperience)
        {
            ExperienceBar = experienceBar;
            Level = level;
            TotalExperience = totalExperience;
        }

        public float ExperienceBar;
        public int Level;
        public int TotalExperience;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            ExperienceBar = stream.ReadSingle();
            Level = stream.ReadVarInt();
            TotalExperience = stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteSingle(ExperienceBar);
            stream.WriteVarInt(Level);
            stream.WriteVarInt(TotalExperience);
            return mode;
        }
    }

    public struct EntityPropertiesPacket : IPacket
    {
        public EntityPropertiesPacket(int entityId, EntityProperty[] properties)
        {
            EntityId = entityId;
            Properties = properties;
        }

        public int EntityId;
        public EntityProperty[] Properties;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            var count = stream.ReadVarInt();
            if (count < 0)
                throw new InvalidOperationException("Cannot specify less than zero properties.");
            Properties = new EntityProperty[count];
            for (int i = 0; i < count; i++)
            {
                var property = new EntityProperty();
                property.Key = stream.ReadString();
                property.Value = stream.ReadDouble();
                var listLength = stream.ReadVarInt();
                property.UnknownList = new EntityPropertyListItem[listLength];
                for (int j = 0; j < listLength; j++)
                {
                    var item = new EntityPropertyListItem();
                    item.UnknownMSB = stream.ReadInt64();
                    item.UnknownLSB = stream.ReadInt64();
                    item.UnknownDouble = stream.ReadDouble();
                    item.UnknownByte = stream.ReadUInt8();
                    property.UnknownList[j] = item;
                }
                Properties[i] = property;
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteVarInt(Properties.Length);
            for (int i = 0; i < Properties.Length; i++)
            {
                stream.WriteString(Properties[i].Key);
                stream.WriteDouble(Properties[i].Value);
                stream.WriteVarInt((short)Properties[i].UnknownList.Length);
                for (int j = 0; j < Properties[i].UnknownList.Length; j++)
                {
                    stream.WriteInt64(Properties[i].UnknownList[j].UnknownMSB);
                    stream.WriteInt64(Properties[i].UnknownList[j].UnknownLSB);
                    stream.WriteDouble(Properties[i].UnknownList[j].UnknownDouble);
                    stream.WriteUInt8(Properties[i].UnknownList[j].UnknownByte);
                }
            }
            return mode;
        }
    }

    public struct EntityProperty
    {
        public EntityProperty(string key, double value)
        {
            Key = key;
            Value = value;
            UnknownList = new EntityPropertyListItem[0];
        }

        public string Key;
        public double Value;
        public EntityPropertyListItem[] UnknownList;
    }

    public struct EntityPropertyListItem
    {
        public long UnknownMSB, UnknownLSB;
        public double UnknownDouble;
        public byte UnknownByte;
    }

    public struct ChunkDataPacket : IPacket
    {
        public ChunkDataPacket(int x, int z, bool groundUpContinuous,
            ushort primaryBitMap, ushort addBitMap, byte[] data)
        {
            X = x;
            Z = z;
            GroundUpContinuous = groundUpContinuous;
            PrimaryBitMap = primaryBitMap;
            AddBitMap = addBitMap;
            Data = data;
        }

        public int X, Z;
        public bool GroundUpContinuous;
        public ushort PrimaryBitMap;
        public ushort AddBitMap;
        public byte[] Data;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            X = stream.ReadInt32();
            Z = stream.ReadInt32();
            GroundUpContinuous = stream.ReadBoolean();
            PrimaryBitMap = stream.ReadUInt16();
            AddBitMap = stream.ReadUInt16();
            var length = stream.ReadInt32();
            Data = stream.ReadUInt8Array(length);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(X);
            stream.WriteInt32(Z);
            stream.WriteBoolean(GroundUpContinuous);
            stream.WriteUInt16(PrimaryBitMap);
            stream.WriteUInt16(AddBitMap);
            stream.WriteInt32(Data.Length);
            stream.WriteUInt8Array(Data);
            return mode;
        }
    }

    public struct MultipleBlockChangePacket : IPacket
    {
        public MultipleBlockChangePacket(int chunkX, int chunkZ, byte[] data)
        {
            // TODO: Make this packet a little nicer
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            Data = data;
            RecordCount = Data.Length;
        }

        public int ChunkX, ChunkZ;
        public byte[] Data;
        public int RecordCount;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            ChunkX = stream.ReadInt32();
            ChunkZ = stream.ReadInt32();
            int RecordCount = stream.ReadVarInt();
            Data = stream.ReadUInt8Array(RecordCount);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(ChunkX);
            stream.WriteInt32(ChunkZ);
            stream.WriteVarInt(RecordCount);
            stream.WriteUInt8Array(Data);
            return mode;
        }
    }

    public struct BlockChangePacket : IPacket
    {
        public BlockChangePacket(Position position, int blockType, byte blockMetadata)
        {
            Pos = position;
            BlockType = blockType;
            BlockMetadata = blockMetadata;
        }

        public Position Pos;
        public int BlockType;
        public byte BlockMetadata;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            BlockType = stream.ReadVarInt();
            BlockMetadata = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            stream.WriteVarInt(BlockType);
            stream.WriteUInt8(BlockMetadata);
            return mode;
        }
    }

    public struct BlockActionPacket : IPacket
    {
        public BlockActionPacket(Position position, byte data1, byte data2, int blockId)
        {
            Pos = position;
            Data1 = data1;
            Data2 = data2;
            BlockId = blockId;
        }

        public Position Pos;
        public byte Data1;
        public byte Data2;
        public int BlockId;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            Data1 = stream.ReadUInt8();
            Data2 = stream.ReadUInt8();
            BlockId = stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            stream.WriteUInt8(Data1);
            stream.WriteUInt8(Data2);
            stream.WriteVarInt(BlockId);
            return mode;
        }
    }

    public struct BlockBreakAnimationPacket : IPacket
    {
        public BlockBreakAnimationPacket(int entityId, Position position, byte destroyStage)
        {
            EntityId = entityId;
            Pos = position;
            DestroyStage = destroyStage;
        }

        public int EntityId;
        public Position Pos;
        public byte DestroyStage;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Pos = new Position(stream.ReadInt64());
            DestroyStage = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteInt64(Pos.Encoded);
            stream.WriteUInt8(DestroyStage);
            return mode;
        }
    }

    public struct MapChunkBulkPacket : IPacket
    {
        public struct Metadata
        {
            public int ChunkX;
            public int ChunkZ;
            public ushort PrimaryBitMap;
            public ushort AddBitMap;
        }

        public MapChunkBulkPacket(short chunkCount, bool lightIncluded, byte[] chunkData, Metadata[] chunkMetadata)
        {
            ChunkCount = chunkCount;
            LightIncluded = lightIncluded;
            ChunkData = chunkData;
            ChunkMetadata = chunkMetadata;
        }

        public short ChunkCount;
        public bool LightIncluded;
        public byte[] ChunkData;
        public Metadata[] ChunkMetadata;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            ChunkCount = stream.ReadInt16();
            var length = stream.ReadInt32();
            LightIncluded = stream.ReadBoolean();
            ChunkData = stream.ReadUInt8Array(length);

            ChunkMetadata = new Metadata[ChunkCount];
            for (int i = 0; i < ChunkCount; i++)
            {
                var metadata = new Metadata();
                metadata.ChunkX = stream.ReadInt32();
                metadata.ChunkZ = stream.ReadInt32();
                metadata.PrimaryBitMap = stream.ReadUInt16();
                metadata.AddBitMap = stream.ReadUInt16();
                ChunkMetadata[i] = metadata;
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt16(ChunkCount);
            stream.WriteInt32(ChunkData.Length);
            stream.WriteBoolean(LightIncluded);
            stream.WriteUInt8Array(ChunkData);

            for (int i = 0; i < ChunkCount; i++)
            {
                stream.WriteInt32(ChunkMetadata[i].ChunkX);
                stream.WriteInt32(ChunkMetadata[i].ChunkZ);
                stream.WriteUInt16(ChunkMetadata[i].PrimaryBitMap);
                stream.WriteUInt16(ChunkMetadata[i].AddBitMap);
            }
            return mode;
        }
    }

    public struct ExplosionPacket : IPacket
    {
        public ExplosionPacket(float x, float y, float z,
            float radius, int recordCount, byte[] records,
            float playerVelocityX, float playerVelocityY, float playerVelocityZ)
        {
            // TODO: Improve this packet
            X = x;
            Y = y;
            Z = z;
            Radius = radius;
            RecordCount = recordCount;
            Records = records;
            PlayerVelocityX = playerVelocityX;
            PlayerVelocityY = playerVelocityY;
            PlayerVelocityZ = playerVelocityZ;
        }

        public float X, Y, Z;
        public float Radius;
        public int RecordCount;
        public byte[] Records;
        public float PlayerVelocityX, PlayerVelocityY, PlayerVelocityZ;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            X = stream.ReadSingle();
            Y = stream.ReadSingle();
            Z = stream.ReadSingle();
            Radius = stream.ReadSingle();
            RecordCount = stream.ReadInt32();
            Records = stream.ReadUInt8Array(RecordCount * 3);
            PlayerVelocityX = stream.ReadSingle();
            PlayerVelocityY = stream.ReadSingle();
            PlayerVelocityZ = stream.ReadSingle();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteSingle(X);
            stream.WriteSingle(Y);
            stream.WriteSingle(Z);
            stream.WriteSingle(Radius);
            stream.WriteInt32(RecordCount);
            stream.WriteUInt8Array(Records);
            stream.WriteSingle(PlayerVelocityX);
            stream.WriteSingle(PlayerVelocityY);
            stream.WriteSingle(PlayerVelocityZ);
            return mode;
        }
    }

    public struct EffectPacket : IPacket
    {
        public EffectPacket(int effectId, Position position, int data, bool disableRelativeVolume)
        {
            EffectId = effectId;
            Pos = position;
            Data = data;
            DisableRelativeVolume = disableRelativeVolume;
        }

        public int EffectId;
        public Position Pos;
        public int Data;
        public bool DisableRelativeVolume;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EffectId = stream.ReadInt32();
            Pos = new Position(stream.ReadInt64());
            Data = stream.ReadInt32();
            DisableRelativeVolume = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(EffectId);
            stream.WriteInt64(Pos.Encoded);
            stream.WriteInt32(Data);
            stream.WriteBoolean(DisableRelativeVolume);
            return mode;
        }
    }

    public struct SoundEffectPacket : IPacket
    {
        public static readonly byte DefaultPitch = 63;
        public static readonly float DefaultVolume = 1.0f;

        public SoundEffectPacket(string soundName, int x, int y,
            int z, float volume, byte pitch)
        {
            SoundName = soundName;
            X = x;
            Y = y;
            Z = z;
            Volume = volume;
            Pitch = pitch;
        }

        public string SoundName;
        public int X, Y, Z;
        public float Volume;
        public byte Pitch;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            SoundName = stream.ReadString();
            X = stream.ReadInt32() / 8;
            Y = stream.ReadInt32() / 8;
            Z = stream.ReadInt32() / 8;
            Volume = stream.ReadSingle();
            Pitch = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(SoundName);
            stream.WriteInt32(X * 8);
            stream.WriteInt32(Y * 8);
            stream.WriteInt32(Z * 8);
            stream.WriteSingle(Volume);
            stream.WriteUInt8(Pitch);
            return mode;
        }
    }

    public struct ParticleEffectPacket : IPacket
    {
        public ParticleEffectPacket(int effectName, bool longDistance, float x, float y, float z,
            float offsetX, float offsetY, float offsetZ, float particleSpeed,
            int particleCount)
        {
            EffectName = effectName;
            LongDistance = longDistance;
            X = x;
            Y = y;
            Z = z;
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
            ParticleSpeed = particleSpeed;
            ParticleCount = particleCount;
        }

        public int EffectName;
        public bool LongDistance;
        public float X, Y, Z;
        public float OffsetX, OffsetY, OffsetZ;
        public float ParticleSpeed;
        public int ParticleCount;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EffectName = stream.ReadInt32();
            LongDistance = stream.ReadBoolean();
            X = stream.ReadSingle();
            Y = stream.ReadSingle();
            Z = stream.ReadSingle();
            OffsetX = stream.ReadSingle();
            OffsetY = stream.ReadSingle();
            OffsetZ = stream.ReadSingle();
            ParticleSpeed = stream.ReadSingle();
            ParticleCount = stream.ReadInt32();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt32(EffectName);
            stream.WriteBoolean(LongDistance);
            stream.WriteSingle(X);
            stream.WriteSingle(Y);
            stream.WriteSingle(Z);
            stream.WriteSingle(OffsetX);
            stream.WriteSingle(OffsetY);
            stream.WriteSingle(OffsetZ);
            stream.WriteSingle(ParticleSpeed);
            stream.WriteInt32(ParticleCount);
            return mode;
        }
    }

    public struct ChangeGameStatePacket : IPacket
    {
        public enum GameState
        {
            InvalidBed = 0,
            EndRaining = 1,
            BeginRaining = 2,
            ChangeGameMode = 3,
            EnterCredits = 4,
            DemoMessages = 5,
            ArrowHitPlayer = 6,
            FadeValue = 7,
            FadeTime = 8
        }

        public ChangeGameStatePacket(GameState state, float value)
        {
            State = state;
            Value = value;
        }

        public GameState State;
        public float Value;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            State = (GameState)stream.ReadUInt8();
            Value = stream.ReadSingle();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8((byte)State);
            stream.WriteSingle(Value);
            return mode;
        }
    }

    public struct SpawnGlobalEntityPacket : IPacket
    {
        public SpawnGlobalEntityPacket(int entityId, byte type, int x, int y, int z)
        {
            EntityId = entityId;
            Type = type;
            X = x;
            Y = y;
            Z = z;
        }

        public int EntityId;
        public byte Type;
        public int X, Y, Z;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Type = stream.ReadUInt8();
            X = stream.ReadInt32();
            Y = stream.ReadInt32();
            Z = stream.ReadInt32();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8(Type);
            stream.WriteInt32(X);
            stream.WriteInt32(Y);
            stream.WriteInt32(Z);
            return mode;
        }
    }

    public struct OpenWindowPacket : IPacket
    {
        public OpenWindowPacket(byte windowId, string inventoryType, string windowTitle,
            byte slotCount, bool useProvidedTitle, int? entityId)
        {
            WindowId = windowId;
            InventoryType = inventoryType;
            WindowTitle = windowTitle;
            SlotCount = slotCount;
            UseProvidedTitle = useProvidedTitle;
            EntityId = entityId;
        }

        public byte WindowId;
        public string InventoryType;
        public string WindowTitle;
        public byte SlotCount;
        public bool UseProvidedTitle;
        public int? EntityId;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadUInt8();
            InventoryType = stream.ReadString();
            WindowTitle = stream.ReadString();
            SlotCount = stream.ReadUInt8();
            UseProvidedTitle = stream.ReadBoolean();
            if (InventoryType == "EntityHorse")
                EntityId = stream.ReadInt32();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8(WindowId);
            stream.WriteString(InventoryType);
            stream.WriteString(WindowTitle);
            stream.WriteUInt8(SlotCount);
            stream.WriteBoolean(UseProvidedTitle);
            if (InventoryType == "EntityHorse")
                stream.WriteInt32(EntityId.GetValueOrDefault());
            return mode;
        }
    }

    public struct CloseWindowPacket : IPacket
    {
        public CloseWindowPacket(sbyte windowId)
        {
            WindowId = windowId;
        }

        public sbyte WindowId;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt8(WindowId);
            return mode;
        }
    }

    public struct ClickWindowPacket : IPacket
    {
        public enum ClickAction
        {
            LeftClick,
            RightClick,
            ShiftLeftClick,
            ShiftRightClick,
            NumKey1, NumKey2, NumKey3, NumKey4, NumKey5, NumKey6, NumKey7, NumKey8, NumKey9,
            MiddleClick,
            Drop,
            DropAll,
            LeftClickEdgeWithEmptyHand,
            RightClickEdgeWithEmptyHand,
            StartLeftClickPaint,
            StartRightClickPaint,
            LeftMousePaintProgress,
            RightMousePaintProgress,
            EndLeftMousePaint,
            EndRightMousePaint,
            DoubleClick,
            Invalid
        }

        public ClickWindowPacket(sbyte windowId, short slotIndex, byte mouseButton, short transactionId,
            byte mode, ItemStack clickedItem) : this()
        {
            WindowId = windowId;
            SlotIndex = slotIndex;
            MouseButton = mouseButton;
            TransactionId = transactionId;
            Mode = mode;
            ClickedItem = clickedItem;
        }

        public sbyte WindowId;
        public short SlotIndex;
        public byte MouseButton;
        public short TransactionId;
        public byte Mode;
        public ItemStack ClickedItem;

        public ClickAction Action { get; private set; }

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadInt8();
            SlotIndex = stream.ReadInt16();
            MouseButton = stream.ReadUInt8();
            TransactionId = stream.ReadInt16();
            Mode = stream.ReadUInt8();
            ClickedItem = ItemStack.FromStream(stream);
            if (Mode == 0)
            {
                if (MouseButton == 0)
                {
                    if (SlotIndex == -999)
                        Action = ClickAction.DropAll;
                    else
                        Action = ClickAction.LeftClick;
                }
                else if (MouseButton == 1)
                {
                    if (SlotIndex == -999)
                        Action = ClickAction.Drop;
                    else
                        Action = ClickAction.RightClick;
                }
                else
                    Action = ClickAction.Invalid;
            }
            else if (Mode == 1)
            {
                if (MouseButton == 0)
                    Action = ClickAction.ShiftLeftClick;
                else if (MouseButton == 1)
                    Action = ClickAction.ShiftRightClick;
                else
                    Action = ClickAction.Invalid;
            }
            else if (Mode == 2)
            {
                if (MouseButton == 0)
                    Action = ClickAction.NumKey1;
                else if (MouseButton == 1)
                    Action = ClickAction.NumKey2;
                else if (MouseButton == 2)
                    Action = ClickAction.NumKey3;
                else if (MouseButton == 3)
                    Action = ClickAction.NumKey4;
                else if (MouseButton == 4)
                    Action = ClickAction.NumKey5;
                else if (MouseButton == 5)
                    Action = ClickAction.NumKey6;
                else if (MouseButton == 6)
                    Action = ClickAction.NumKey7;
                else if (MouseButton == 7)
                    Action = ClickAction.NumKey8;
                else if (MouseButton == 8)
                    Action = ClickAction.NumKey9;
                else
                    Action = ClickAction.Invalid;
            }
            else if (Mode == 3)
            {
                if (MouseButton == 2)
                    Action = ClickAction.MiddleClick;
                else
                    Action = ClickAction.Invalid;
            }
            else if (Mode == 4)
            {
                if (SlotIndex == -999)
                {
                    if (Mode == 0)
                        Action = ClickAction.LeftClickEdgeWithEmptyHand;
                    else if (Mode == 1)
                        Action = ClickAction.RightClickEdgeWithEmptyHand;
                    else
                        Action = ClickAction.Invalid;
                }
                else
                {
                    if (Mode == 0)
                        Action = ClickAction.Drop;
                    else if (Mode == 1)
                        Action = ClickAction.DropAll;
                    else
                        Action = ClickAction.Invalid;
                }
            }
            else if (Mode == 5)
            {
                if (MouseButton == 0)
                    Action = ClickAction.StartLeftClickPaint;
                else if (MouseButton == 1)
                    Action = ClickAction.LeftMousePaintProgress;
                else if (MouseButton == 2)
                    Action = ClickAction.EndLeftMousePaint;
                else if (MouseButton == 4)
                    Action = ClickAction.StartRightClickPaint;
                else if (MouseButton == 5)
                    Action = ClickAction.RightMousePaintProgress;
                else if (MouseButton == 6)
                    Action = ClickAction.EndRightMousePaint;
                else
                    Action = ClickAction.Invalid;
            }
            else if (Mode == 6)
                Action = ClickAction.DoubleClick;
            else
                Action = ClickAction.Invalid;
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt8(WindowId);
            stream.WriteInt16(SlotIndex);
            stream.WriteUInt8(MouseButton);
            stream.WriteInt16(TransactionId);
            stream.WriteUInt8(Mode);
            ClickedItem.WriteTo(stream);
            return mode;
        }
    }

    public struct SetSlotPacket : IPacket
    {
        public SetSlotPacket(byte windowId, short slotIndex, ItemStack item)
        {
            WindowId = windowId;
            SlotIndex = slotIndex;
            Item = item;
        }

        public byte WindowId;
        public short SlotIndex;
        public ItemStack Item;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadUInt8();
            SlotIndex = stream.ReadInt16();
            Item = ItemStack.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8(WindowId);
            stream.WriteInt16(SlotIndex);
            Item.WriteTo(stream);
            return mode;
        }
    }

    public struct SetWindowItemsPacket : IPacket
    {
        public SetWindowItemsPacket(byte windowId, ItemStack[] items)
        {
            WindowId = windowId;
            Items = items;
        }

        public byte WindowId;
        public ItemStack[] Items;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadUInt8();
            short count = stream.ReadInt16();
            Items = new ItemStack[count];
            for (int i = 0; i < count; i++)
                Items[i] = ItemStack.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8(WindowId);
            stream.WriteInt16((short)Items.Length);
            for (int i = 0; i < Items.Length; i++)
                Items[i].WriteTo(stream);
            return mode;
        }
    }

    public struct UpdateWindowPropertyPacket : IPacket
    {
        public UpdateWindowPropertyPacket(byte windowId, short propertyId, short value)
        {
            WindowId = windowId;
            PropertyId = propertyId;
            Value = value;
        }

        public byte WindowId;
        public short PropertyId;
        public short Value;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadUInt8();
            PropertyId = stream.ReadInt16();
            Value = stream.ReadInt16();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8(WindowId);
            stream.WriteInt16(PropertyId);
            stream.WriteInt16(Value);
            return mode;
        }
    }

    public struct ConfirmTransactionPacket : IPacket
    {
        public ConfirmTransactionPacket(byte windowId, short actionNumber, bool accepted)
        {
            WindowId = windowId;
            ActionNumber = actionNumber;
            Accepted = accepted;
        }

        public byte WindowId;
        public short ActionNumber;
        public bool Accepted;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadUInt8();
            ActionNumber = stream.ReadInt16();
            Accepted = stream.ReadBoolean();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8(WindowId);
            stream.WriteInt16(ActionNumber);
            stream.WriteBoolean(Accepted);
            return mode;
        }
    }

    public struct UpdateSignPacket : IPacket
    {
        public UpdateSignPacket(Position position, string text1, string text2, string text3, string text4)
        {
            Pos = position;
            Text1 = text1;
            Text2 = text2;
            Text3 = text3;
            Text4 = text4;
        }

        public Position Pos;
        public string Text1, Text2, Text3, Text4;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            Text1 = stream.ReadString();
            Text2 = stream.ReadString();
            Text3 = stream.ReadString();
            Text4 = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            stream.WriteString(Text1);
            stream.WriteString(Text2);
            stream.WriteString(Text3);
            stream.WriteString(Text4);
            return mode;
        }
    }

    public struct MapDataPacket : IPacket
    {
        public MapDataPacket(int metadata, byte[] data)
        {
            Metadata = metadata;
            Data = data;
        }

        public int Metadata;
        public byte[] Data;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Metadata = stream.ReadVarInt();
            var length = stream.ReadInt16();
            Data = stream.ReadUInt8Array(length);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Metadata);
            stream.WriteInt16((short)Data.Length);
            stream.WriteUInt8Array(Data);
            return mode;
        }
    }

    public struct UpdateTileEntityPacket : IPacket
    {
        public UpdateTileEntityPacket(Position position, byte action, NbtFile nbt)
        {
            Pos = position;
            Action = action;
            Nbt = nbt;
        }

        public Position Pos;
        public byte Action;
        public NbtFile Nbt;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            Action = stream.ReadUInt8();
            var length = stream.ReadInt16();
            var data = stream.ReadUInt8Array(length);
            Nbt = new NbtFile();
            Nbt.LoadFromBuffer(data, 0, length, NbtCompression.GZip, null);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            stream.WriteUInt8(Action);
            var tempStream = new MemoryStream();
            Nbt.SaveToStream(tempStream, NbtCompression.GZip);
            var buffer = tempStream.GetBuffer();
            stream.WriteVarInt(buffer.Length);
            stream.WriteUInt8Array(buffer);
            return mode;
        }
    }

    public struct OpenSignEditorPacket : IPacket
    {
        public OpenSignEditorPacket(Position position)
        {
            Pos = position;
        }

        public Position Pos;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            return mode;
        }
    }

    public struct Statistic
    {
        public Statistic(string name, int value)
        {
            Name = name;
            Value = value;
        }

        public string Name;
        public int Value;
    }

    public struct UpdateStatisticsPacket : IPacket
    {
        public UpdateStatisticsPacket(Statistic[] statistics)
        {
            Statistics = statistics;
        }

        public Statistic[] Statistics;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            var length = stream.ReadVarInt();
            Statistics = new Statistic[length];
            for (long i = 0; i < length; i++)
            {
                Statistics[i] = new Statistic(
                    stream.ReadString(),
                    stream.ReadVarInt());
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Statistics.Length);
            for (long i = 0; i < Statistics.LongLength; i++)
            {
                stream.WriteString(Statistics[i].Name);
                stream.WriteVarInt(Statistics[i].Value);
            }
            return mode;
        }
    }

    public struct PlayerListItemPacket : IPacket
    {
        public PlayerListItemPacket(int numberOfPlayers, string uuid, string name, int numberOfProperties, string[] property, int gamemode, int ping, string displayName)
        {
            Action = 0;
            NumberOfPlayers = numberOfPlayers;
            UUID = uuid;

            Name = name;
            NumberOfProperties = numberOfProperties;
            Property = property;
            Gamemode = gamemode;
            Ping = ping;
            DisplayName = displayName;
        }

        public PlayerListItemPacket(int action, int numberOfPlayers, string uuid, int gamemodeOrPing)
        {
            Action = action;
            NumberOfPlayers = numberOfPlayers;
            UUID = uuid;

            Name = null;
            NumberOfProperties = -1;
            Property = null;
            Gamemode = -1;
            Ping = -1;
            DisplayName = null;

            if (Action == 1)
            {
                Gamemode = gamemodeOrPing;
            }
            else
            {
                Ping = gamemodeOrPing;
            }
        }

        public PlayerListItemPacket(int numberOfPlayers, string uuid, string displayName)
        {
            Action = 3;
            NumberOfPlayers = numberOfPlayers;
            UUID = uuid;

            Name = null;
            NumberOfProperties = -1;
            Property = null;
            Gamemode = -1;
            Ping = -1;
            DisplayName = null;

            if (displayName != null)
            {
                DisplayName = displayName;
            }
        }

        public PlayerListItemPacket(int numberOfPlayers, string uuid)
        {
            Action = 4;
            NumberOfPlayers = numberOfPlayers;
            UUID = uuid;

            Name = null;
            NumberOfProperties = -1;
            Property = null;
            Gamemode = -1;
            Ping = -1;
            DisplayName = null;
        }

        public int Action;
        public int NumberOfPlayers;

        public string UUID;

        public string Name;
        public int NumberOfProperties;
        public string[] Property;
        public int Gamemode;
        public int Ping;

        public string DisplayName;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Action = stream.ReadVarInt();
            NumberOfPlayers = stream.ReadVarInt();
            UUID = stream.ReadString();

            if (Action == 0)
            {
                Name = stream.ReadString();
                NumberOfProperties = stream.ReadVarInt();
                Property[0] = stream.ReadString();
                Property[1] = stream.ReadString();
                if (stream.ReadBoolean() == true)
                {
                    Property[2] = stream.ReadString();
                }
                Gamemode = stream.ReadVarInt();
                Ping = stream.ReadVarInt();
                if (stream.ReadBoolean() == true)
                {
                    DisplayName = stream.ReadString();
                }
            }
            if (Action == 1)
            {
                Gamemode = stream.ReadVarInt();
            }
            if (Action == 2)
            {
                Ping = stream.ReadVarInt();
            }
            if (Action == 3)
            {
                if (stream.ReadBoolean() == true)
                {
                    DisplayName = stream.ReadString();
                }
            }
            if (Action == 4)
            {
                //Remove Player
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            return mode;
        }
    }

    public struct PlayerAbilitiesPacket : IPacket
    {
        public PlayerAbilitiesPacket(byte flags, float flyingSpeed, float walkingSpeed)
        {
            Flags = flags;
            FlyingSpeed = flyingSpeed;
            WalkingSpeed = walkingSpeed;
        }

        public byte Flags;
        public float FlyingSpeed, WalkingSpeed;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Flags = stream.ReadUInt8();
            FlyingSpeed = stream.ReadSingle();
            WalkingSpeed = stream.ReadSingle();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8(Flags);
            stream.WriteSingle(FlyingSpeed);
            stream.WriteSingle(WalkingSpeed);
            return mode;
        }
    }

    public struct TabCompletePacket : IPacket
    {
        public TabCompletePacket(string text, bool hasPosition, ulong lookedAtBlock)
        {
            Text = text;
            HasPosition = hasPosition;
            if (HasPosition)
                LookedAtBlock = lookedAtBlock;
            else
                LookedAtBlock = 0;
            Completions = null;
        }

        public TabCompletePacket(string[] completions)
        {
            Completions = completions;
            Text = null;
            HasPosition = false;
            LookedAtBlock = 0;
        }

        public string[] Completions;
        public string Text;
        bool HasPosition;
        ulong LookedAtBlock;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            if (direction == PacketDirection.Clientbound)
            {
                var count = stream.ReadVarInt();
                Completions = new string[count];
                for (long i = 0; i < Completions.LongLength; i++)
                    Completions[i] = stream.ReadString();
            }
            else
            {
                Text = stream.ReadString();
                HasPosition = stream.ReadBoolean();
                if (HasPosition)
                    LookedAtBlock = stream.ReadUInt64();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            if (direction == PacketDirection.Clientbound)
            {
                stream.WriteVarInt(Completions.Length);
                for (long i = 0; i < Completions.Length; i++)
                    stream.WriteString(Completions[i]);
            }
            else
            {
                stream.WriteString(Text);
                stream.WriteBoolean(HasPosition);
                if (HasPosition)
                    stream.WriteUInt64(LookedAtBlock);
            }
            return mode;
        }
    }

    public struct ScoreboardObjectivePacket : IPacket
    {
        public enum UpdateMode
        {
            Create = 0,
            Remove = 1,
            Update = 2
        }

        public ScoreboardObjectivePacket(string name, UpdateMode mode, string value, string type)
        {
            Name = name;
            Mode = mode;
            if (Mode == UpdateMode.Create && Mode == UpdateMode.Update)
            {
                Value = value;
                Type = type;
            }
            else
            {
                Value = "";
                Type = "";
            }
        }

        public string Name;
        public UpdateMode Mode;
        public string Value;
        public string Type;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Name = stream.ReadString();
            Mode = (UpdateMode)stream.ReadUInt8();
            if (Mode == UpdateMode.Create && Mode == UpdateMode.Update)
            {
                Value = stream.ReadString();
                Type = stream.ReadString();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Name);
            stream.WriteUInt8((byte)Mode);
            if (Mode == UpdateMode.Create && Mode == UpdateMode.Update)
            {
                stream.WriteString(Value);
                stream.WriteString(Type);
            }
            return mode;
        }
    }

    public struct UpdateScorePacket : IPacket
    {

        public UpdateScorePacket(string name)
        {
            Name = name;
            Action = true;
            ObjectiveName = null;
            Value = null;
        }

        public UpdateScorePacket(string name, string objectiveName, int value)
        {
            Name = name;
            Action = false;
            ObjectiveName = objectiveName;
            Value = value;
        }

        public string Name;
        public bool Action;
        public string ObjectiveName;
        public int? Value;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Name = stream.ReadString();
            Action = stream.ReadBoolean();
            if (!Action)
            {
                ObjectiveName = stream.ReadString();
                Value = stream.ReadVarInt();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Name);
            stream.WriteBoolean(Action);
            if (!Action)
            {
                stream.WriteString(ObjectiveName);
                stream.WriteVarInt(Value.Value);
            }
            return mode;
        }
    }

    public struct DisplayScoreboardPacket : IPacket
    {
        public DisplayScoreboardPacket(ScoreboardPosition position, string scoreName)
        {
            Position = position;
            ScoreName = scoreName;
        }

        public enum ScoreboardPosition
        {
            PlayerList = 0,
            Sidebar = 1,
            BelowPlayerName = 2
        }

        public ScoreboardPosition Position;
        public string ScoreName;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Position = (ScoreboardPosition)stream.ReadUInt8();
            ScoreName = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8((byte)Position);
            stream.WriteString(ScoreName);
            return mode;
        }
    }

    public struct SetTeamsPacket : IPacket
    {
        public static SetTeamsPacket CreateTeam(string teamName, string displayName, string teamPrefix,
                                                string teamSuffix, bool enableFriendlyFire, string nameTagVisibility, byte color, string[] players)
        {
            var packet = new SetTeamsPacket();
            packet.PacketMode = TeamMode.CreateTeam;
            packet.TeamName = teamName;
            packet.DisplayName = displayName;
            packet.TeamPrefix = teamPrefix;
            packet.TeamSuffix = teamSuffix;
            packet.EnableFriendlyFire = enableFriendlyFire;
            packet.NameTagVisibility = nameTagVisibility;
            packet.Color = color;
            packet.Players = players;
            return packet;
        }

        public static SetTeamsPacket UpdateTeam(string teamName, string displayName, string teamPrefix,
                                                string teamSuffix, bool enableFriendlyFire, string nameTagVisibility, byte color)
        {
            var packet = new SetTeamsPacket();
            packet.PacketMode = TeamMode.UpdateTeam;
            packet.TeamName = teamName;
            packet.DisplayName = displayName;
            packet.TeamPrefix = teamPrefix;
            packet.TeamSuffix = teamSuffix;
            packet.EnableFriendlyFire = enableFriendlyFire;
            packet.NameTagVisibility = nameTagVisibility;
            packet.Color = color;
            return packet;
        }

        public static SetTeamsPacket RemoveTeam(string teamName)
        {
            var packet = new SetTeamsPacket();
            packet.PacketMode = TeamMode.RemoveTeam;
            packet.TeamName = teamName;
            return packet;
        }

        public static SetTeamsPacket AddPlayers(string teamName, string[] players)
        {
            var packet = new SetTeamsPacket();
            packet.PacketMode = TeamMode.AddPlayers;
            packet.TeamName = teamName;
            packet.Players = players;
            return packet;
        }

        public static SetTeamsPacket RemovePlayers(string teamName, string[] players)
        {
            var packet = new SetTeamsPacket();
            packet.PacketMode = TeamMode.RemovePlayers;
            packet.TeamName = teamName;
            packet.Players = players;
            return packet;
        }

        public enum TeamMode
        {
            CreateTeam = 0,
            RemoveTeam = 1,
            UpdateTeam = 2,
            AddPlayers = 3,
            RemovePlayers = 4
        }

        public string TeamName;
        public TeamMode PacketMode;
        public string DisplayName;
        public string TeamPrefix;
        public string TeamSuffix;
        public bool? EnableFriendlyFire;
        public string NameTagVisibility;
        public byte Color;
        public string[] Players;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            TeamName = stream.ReadString();
            PacketMode = (TeamMode)stream.ReadUInt8();
            if (PacketMode == TeamMode.CreateTeam || PacketMode == TeamMode.UpdateTeam)
            {
                DisplayName = stream.ReadString();
                TeamPrefix = stream.ReadString();
                TeamSuffix = stream.ReadString();
                EnableFriendlyFire = stream.ReadBoolean();
                NameTagVisibility = stream.ReadString();
                Color = (byte)stream.ReadByte();
            }
            if (PacketMode == TeamMode.CreateTeam || PacketMode == TeamMode.AddPlayers ||
                PacketMode == TeamMode.RemovePlayers)
            {
                var playerCount = stream.ReadVarInt();
                Players = new string[playerCount];
                for (int i = 0; i < playerCount; i++)
                    Players[i] = stream.ReadString();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(TeamName);
            stream.WriteUInt8((byte)PacketMode);
            if (PacketMode == TeamMode.CreateTeam || PacketMode == TeamMode.UpdateTeam)
            {
                stream.WriteString(DisplayName);
                stream.WriteString(TeamPrefix);
                stream.WriteString(TeamSuffix);
                stream.WriteBoolean(EnableFriendlyFire.Value);
                stream.WriteString(NameTagVisibility);
                stream.WriteByte(Color);
            }
            if (PacketMode == TeamMode.CreateTeam || PacketMode == TeamMode.AddPlayers ||
                PacketMode == TeamMode.RemovePlayers)
            {
                stream.WriteVarInt((short)Players.Length);
                for (int i = 0; i < Players.Length; i++)
                    stream.WriteString(Players[i]);
            }
            return mode;
        }
    }

    public struct PluginMessagePacket : IPacket
    {
        public PluginMessagePacket(string channel, byte[] data)
        {
            Channel = channel;
            Data = data;
        }

        public string Channel;
        public byte[] Data;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Channel = stream.ReadString();
            //var length = stream.ReadInt16();
            Data = stream.ReadUInt8Array((int)(stream.Length - ASCIIEncoding.Unicode.GetByteCount(Channel)));
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Channel);
            //stream.WriteInt16((short)Data.Length);
            stream.WriteUInt8Array(Data);
            return mode;
        }
    }

    public struct DisconnectPacket : IPacket
    {
        public DisconnectPacket(string reason)
        {
            Reason = reason;
        }

        public string Reason;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Reason = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Reason);
            return mode;
        }
    }

    public struct UseEntityPacket : IPacket
    {
        public UseEntityPacket(int target, int type, float targetX, float targetY, float targetZ)
        {
            Target = target;
            Type = type;

            if (Type == 2)
            {
                TargetX = targetX;
                TargetY = targetY;
                TargetZ = targetZ;
            }
            else
            {
                TargetX = 0;
                TargetY = 0;
                TargetZ = 0;
            }
        }

        public int Target;
        public int Type;

        public float TargetX;
        public float TargetY;
        public float TargetZ;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Target = stream.ReadVarInt();
            Type = stream.ReadVarInt();
            if (Type == 2)
            {
                TargetX = stream.ReadInt32();
                TargetY = stream.ReadInt32();
                TargetZ = stream.ReadInt32();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Target);
            stream.WriteVarInt(Type);
            if (Type == 2)
            {
                stream.WriteInt32((int)TargetX);
                stream.WriteInt32((int)TargetY);
                stream.WriteInt32((int)TargetZ);
            }
            return mode;
        }
    }

    public struct PlayerBlockActionPacket : IPacket
    {
        public enum BlockAction
        {
            StartDigging = 0,
            CancelDigging = 1,
            FinishDigging = 2,
            DropItemStack = 3,
            DropItem = 4,
            NOP = 5
        }

        public PlayerBlockActionPacket(BlockAction action, Position position, BlockFace face)
        {
            Action = action;
            Pos = position;
            Face = face;
        }

        public BlockAction Action;
        public Position Pos;
        public BlockFace Face;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Action = (BlockAction)stream.ReadInt8();
            Pos = new Position(stream.ReadInt64());
            Face = (BlockFace)stream.ReadInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt8((sbyte)Action);
            stream.WriteInt64(Pos.Encoded);
            stream.WriteInt8((sbyte)Face);
            return mode;
        }
    }

    public struct RightClickPacket : IPacket
    {
        public RightClickPacket(Position position, BlockFace direction, ItemStack heldItem, sbyte cursorX, sbyte cursorY, sbyte cursorZ)
        {
            Pos = position;
            Face = direction;
            HeldItem = heldItem;
            CursorX = cursorX;
            CursorY = cursorY;
            CursorZ = cursorZ;
        }

        public Position Pos;
        public BlockFace Face;
        public ItemStack HeldItem;
        public sbyte CursorX, CursorY, CursorZ;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Pos = new Position(stream.ReadInt64());
            Face = (BlockFace)stream.ReadUInt8();
            HeldItem = ItemStack.FromStream(stream);
            CursorX = stream.ReadInt8();
            CursorY = stream.ReadInt8();
            CursorZ = stream.ReadInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt64(Pos.Encoded);
            stream.WriteUInt8((byte)Face);
            HeldItem.WriteTo(stream);
            stream.WriteInt8(CursorX);
            stream.WriteInt8(CursorY);
            stream.WriteInt8(CursorZ);
            return mode;
        }
    }

    public struct EntityActionPacket : IPacket
    {
        public enum EntityAction
        {
            StartSneaking = 0,
            StopSneaking = 1,
            LeaveBed = 2,
            StartSprinting = 3,
            StopSprinting = 4,
            JumpWithHorse = 5,
            OpenRiddenHorseInventory = 6
        }

        public EntityActionPacket(int entityId, EntityAction action, int jumpBoost)
        {
            EntityId = entityId;
            Action = action;
            JumpBoost = jumpBoost;
        }

        public int EntityId;
        public EntityAction Action;
        public int JumpBoost;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityId = stream.ReadVarInt();
            Action = (EntityAction)stream.ReadUInt8();
            JumpBoost = stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityId);
            stream.WriteUInt8((byte)Action);
            stream.WriteVarInt(JumpBoost);
            return mode;
        }
    }

    public struct SteerVehiclePacket : IPacket
    {
        public SteerVehiclePacket(float sideways, float forward, byte flags)
        {
            Sideways = sideways;
            Forward = forward;
            Flags = flags;
        }

        //Sideways: Left=Positive, Right=Negative //Foward: Foward=Positve, Backwards=Negative
        public float Sideways, Forward;
        //Flags: 0x1: Jump, 0x2: Unmount
        public byte Flags;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Sideways = stream.ReadSingle();
            Forward = stream.ReadSingle();
            Flags = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteSingle(Sideways);
            stream.WriteSingle(Forward);
            stream.WriteUInt8(Flags);
            return mode;
        }
    }

    public struct CreativeInventoryActionPacket : IPacket
    {
        public CreativeInventoryActionPacket(short slot, ItemStack item)
        {
            Slot = slot;
            Item = item;
        }

        public short Slot;
        public ItemStack Item;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Slot = stream.ReadInt16();
            Item = ItemStack.FromStream(stream);
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt16(Slot);
            Item.WriteTo(stream);
            return mode;
        }
    }

    public struct EnchantItemPacket : IPacket
    {
        public EnchantItemPacket(sbyte windowId, sbyte enchantmentIndex)
        {
            WindowId = windowId;
            EnchantmentIndex = enchantmentIndex;
        }

        public sbyte WindowId;
        public sbyte EnchantmentIndex;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            WindowId = stream.ReadInt8();
            EnchantmentIndex = stream.ReadInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteInt8(WindowId);
            stream.WriteInt8(EnchantmentIndex);
            return mode;
        }
    }

    public struct ClientSettingsPacket : IPacket
    {
        public enum ChatMode
        {
            Hidden = 2,
            CommandsOnly = 1,
            Enabled = 0
        }

        public ClientSettingsPacket(string locale, byte viewDistance, ChatMode chatFlags, bool colorEnabled, byte showCape)
        {
            Locale = locale;
            ViewDistance = viewDistance;
            ChatFlags = chatFlags;
            ColorEnabled = colorEnabled;
            //Difficulty = difficulty;
            ShowCape = showCape;
        }

        public string Locale;
        public byte ViewDistance;
        public ChatMode ChatFlags;
        public bool ColorEnabled;
        //public Difficulty Difficulty;
        public byte ShowCape;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Locale = stream.ReadString();
            ViewDistance = stream.ReadUInt8();
            var flags = stream.ReadUInt8();
            ChatFlags = (ChatMode)(flags & 0x3);
            ColorEnabled = stream.ReadBoolean();
            //Difficulty = (Difficulty)stream.ReadUInt8();
            ShowCape = stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Locale);
            stream.WriteUInt8(ViewDistance);
            stream.WriteUInt8((byte)ChatFlags);
            stream.WriteBoolean(ColorEnabled);
            //stream.WriteUInt8((byte)Difficulty);
            stream.WriteUInt8(ShowCape);
            return mode;
        }
    }

    public struct ServerDifficultyPacket : IPacket
    {
        public ServerDifficultyPacket(Difficulty difficulty)
        {
            Difficulty = difficulty;
        }

        public Difficulty Difficulty;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Difficulty = (Difficulty)stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8((byte)Difficulty);
            return mode;
        }
    }

    public struct ClientStatusPacket : IPacket
    {
        public enum StatusChange
        {
            Respawn = 0,
            RequestStats = 1,
            OpenInventory = 2 // Used to acquire the relevant achievement
        }

        public ClientStatusPacket(StatusChange change)
        {
            Change = change;
        }

        public StatusChange Change;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Change = (StatusChange)stream.ReadUInt8();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteUInt8((byte)Change);
            return mode;
        }
    }

    public struct CombatEventPacket : IPacket
    {
        public enum CombatEvent
        {
            EnterCombat = 0,
            EndCombat = 1,
            EntityDead = 2
        }

        public CombatEventPacket(CombatEvent combatEvent)
        {
            Event = combatEvent;
            Duration = 0;
            PlayerID = -1;
            EntityID = -1;
            Message = null;
        }

        public CombatEventPacket(CombatEvent combatEvent, int duration, int entityID)
        {
            Event = combatEvent;
            Duration = duration;
            PlayerID = -1;
            EntityID = entityID;
            Message = null;
        }

        public CombatEventPacket(CombatEvent combatEvent, int playerID, int entityID, string message)
        {
            Event = combatEvent;
            Duration = 0;
            PlayerID = playerID;
            EntityID = entityID;
            Message = message;
        }


        public CombatEventPacket(CombatEvent combatEvent, int duration, int playerID, int entityID, string message)
        {
            Event = combatEvent;
            Duration = duration;
            PlayerID = playerID;
            EntityID = entityID;
            Message = message;
        }

        public CombatEvent Event;
        public int Duration;
        public int PlayerID;
        public int EntityID;
        public string Message;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Event = (CombatEvent)stream.ReadVarInt();
            if (Event == CombatEvent.EndCombat)
            {
                Duration = stream.ReadVarInt();
                EntityID = stream.ReadInt32();
            }
            if (Event == CombatEvent.EntityDead)
            {
                PlayerID = stream.ReadVarInt();
                EntityID = stream.ReadInt32();
                Message = stream.ReadString();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt((int)Event);
            if (Event == CombatEvent.EndCombat)
            {
                stream.WriteVarInt(Duration);
                stream.WriteInt32(EntityID);
            }
            if (Event == CombatEvent.EntityDead)
            {
                stream.WriteVarInt(PlayerID);
                stream.WriteInt32(EntityID);
                stream.WriteString(Message);
            }
            return mode;
        }
    }

    public struct CameraPacket : IPacket
    {
        public CameraPacket(int cameraID)
        {
            CameraID = cameraID;
        }

        public int CameraID;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            return mode;
        }
    }

    public struct WorldBorderPacket : IPacket
    {
        //Action==0
        public WorldBorderPacket(double radius)
        {
            Action = 0;
            
            X = 0;
            Z = 0;
            Radius = radius;
            OldRadius = 0;
            NewRadius = 0;
            Speed = 0;
            PortalTeleportBoundary = 0;
            WarningTime = 0;
            WarningBlocks = 0;
        }

        //Action==1
        public WorldBorderPacket(double oldRadius, double newRadius, long speed)
        {
            Action = 1;
            
            X = 0;
            Z = 0;
            Radius = 0;
            OldRadius = oldRadius;
            NewRadius = newRadius;
            Speed = speed;
            PortalTeleportBoundary = 0;
            WarningTime = 0;
            WarningBlocks = 0;
        }

        //Action==2
        public WorldBorderPacket(double x, double z)
        {
            Action = 2;

            X = x;
            Z = z;
            Radius = 0;
            OldRadius = 0;
            NewRadius = 0;
            Speed = 0;
            PortalTeleportBoundary = 0;
            WarningTime = 0;
            WarningBlocks = 0;
        }

        //Action==3
        public WorldBorderPacket(double x, double z, double oldRadius, double newRadius, long speed, int portalTeleportBoundary, int warningTime, int warningBlocks)
        {
            Action = 3;

            X = x;
            Z = z;
            Radius = 0;
            OldRadius = oldRadius;
            NewRadius = newRadius;
            Speed = speed;
            PortalTeleportBoundary = portalTeleportBoundary;
            WarningTime = warningTime;
            WarningBlocks = warningBlocks;
        }

        //Action==4 && 5
        public WorldBorderPacket(int action, int warningTimeOrBlocks)
        {
            Action = action;
            if(Action == 4)
            {
                WarningTime = warningTimeOrBlocks;
                WarningBlocks = 0;
            }
            else
            {
                WarningTime = 0;
                WarningBlocks = warningTimeOrBlocks;
            }

            X = 0;
            Z = 0;
            Radius = 0;
            OldRadius = 0;
            NewRadius = 0;
            Speed = 0;
            PortalTeleportBoundary = 0;
            WarningTime = 0;
            WarningBlocks = 0;
        }

        public int Action;
        public double Radius, X, Z, OldRadius, NewRadius;
        public long Speed; 
        public int PortalTeleportBoundary, WarningTime, WarningBlocks;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Action = stream.ReadVarInt();
            if (Action == 0)
            {
                Radius = stream.ReadDouble();
            }
            if (Action == 1)
            {
                OldRadius = stream.ReadDouble();
                NewRadius = stream.ReadDouble();
                Speed = stream.ReadVarLong();
            }
            if (Action == 2)
            {
                X = stream.ReadDouble();
                Z = stream.ReadDouble();
            }
            if (Action == 3)
            {
                X = stream.ReadDouble();
                Z = stream.ReadDouble();
                OldRadius = stream.ReadDouble();
                NewRadius = stream.ReadDouble();
                Speed = stream.ReadVarLong();
                PortalTeleportBoundary = stream.ReadVarInt();
                WarningTime = stream.ReadVarInt();
                WarningBlocks = stream.ReadVarInt();
            }
            if (Action == 4)
            {
                WarningTime = stream.ReadVarInt();
            }
            if (Action == 5)
            {
                WarningBlocks = stream.ReadVarInt();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Action);
            if (Action == 0)
            {
                stream.WriteDouble(Radius);
            }
            if (Action == 1)
            {
                stream.WriteDouble(OldRadius);
                stream.WriteDouble(NewRadius);
                stream.WriteVarLong(Speed);
            }
            if (Action == 2)
            {
                stream.WriteDouble(X);
                stream.WriteDouble(Z);
            }
            if (Action == 3)
            {
                stream.WriteDouble(X);
                stream.WriteDouble(Z);
                stream.WriteDouble(OldRadius);
                stream.WriteDouble(NewRadius);
                stream.WriteVarLong(Speed);
                stream.WriteVarInt(PortalTeleportBoundary);
                stream.WriteVarInt(WarningTime);
                stream.WriteVarInt(WarningBlocks);
            }
            if (Action == 4)
            {
                stream.WriteVarInt(WarningTime);
            }
            if (Action == 5)
            {
                stream.WriteVarInt(WarningBlocks);
            }
            return mode;
        }
    }

    public struct TitlePacket : IPacket
    {
        //Action==0 && 1
        public TitlePacket(int action, string text)
        {
            Action = action;
            if (Action == 0)
            {
                TitleText = text;
                SubtitleText = null;
            }
            else
            {
                TitleText = null;
                SubtitleText = text;
            }
            FadeIn = 0;
            Stay = 0;
            FadeOut = 0;
        }

        //Action==2
        public TitlePacket(int fadeIn, int stay, int fadeOut)
        {
            Action = 2;
            TitleText = null;
            SubtitleText = null;

            FadeIn = fadeIn;
            Stay = stay;
            FadeOut = fadeOut;
        }

        //Action==3 && 4
        public TitlePacket(int action)
        {
            Action = action;
            TitleText = null;
            SubtitleText = null;
            FadeIn = 0;
            Stay = 0;
            FadeOut = 0;
        }

        public int Action;

        //Only Set When Action==0
        public string TitleText;
        //Only Set When Action==1
        public string SubtitleText;

        //Number of ticks //Only Set When Action==2
        public int FadeIn;
        public int Stay;
        public int FadeOut;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Action = stream.ReadVarInt();
            if (Action == 0)
            {
                TitleText = stream.ReadString();
            }
            if (Action == 1)
            {
                SubtitleText = stream.ReadString();
            }
            if (Action == 2)
            {
                FadeIn = stream.ReadInt32();
                Stay = stream.ReadInt32();
                FadeOut = stream.ReadInt32();
            }
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Action);
            if (Action == 0)
            {
                stream.WriteString(TitleText);
            }
            if (Action == 1)
            {
                stream.WriteString(SubtitleText);
            }
            if (Action == 2)
            {
                stream.WriteInt32(FadeIn);
                stream.WriteInt32(Stay);
                stream.WriteInt32(FadeOut);
            }
            return mode;
        }
    }

    //"This packet is completely broken and has been removed in the 1.9 snapshots. The packet Set Compression (Login, 0x03, clientbound) should be used instead."
    [Obsolete("SetCompressionPacket is deprecated, please use SetCompression(Login, 0x03, clientbound) instead.")]
    public struct SetCompressionPacket : IPacket
    {
        public SetCompressionPacket(int threshhold)
        {
            Threshhold = threshhold;
        }

        public int Threshhold;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Threshhold = stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(Threshhold);
            return mode;
        }
    }

    public struct PlayerListHeadAndFooterPacket : IPacket
    {
        public PlayerListHeadAndFooterPacket(string header, string footer)
        {
            Header = header;
            Footer = footer;
        }

        public string Header, Footer;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Header = stream.ReadString();
            Footer = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Header);
            stream.WriteString(Footer);
            return mode;
        }
    }

    public struct ResourcePackSend : IPacket
    {
        public ResourcePackSend(string url, string hash)
        {
            URL = url;
            Hash = hash;
        }

        public string URL;
        public string Hash;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            URL = stream.ReadString();
            Hash = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(URL);
            stream.WriteString(Hash);
            return mode;
        }
    }

    public struct ResourcePackStatus : IPacket
    {
        public enum ResultEnum
        {
            SuccessfullyLoaded = 0,
            Declined = 1,
            FailedDownload = 2,
            Accepted = 3
        }

        public ResourcePackStatus(string hash, ResultEnum result)
        {
            Hash = hash;
            Result = result;
        }

        public string Hash;
        public ResultEnum Result;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            Hash = stream.ReadString();
            Result = (ResultEnum)stream.ReadVarInt();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteString(Hash);
            stream.WriteVarInt((int)Result);
            return mode;
        }
    }

    public struct UpdateEntityNBTPacket : IPacket
    {
        //Find out more on NBT
        public UpdateEntityNBTPacket(int entityID, string tag)
        {
            EntityID = entityID;
            Tag = tag;
        }

        public int EntityID;
        public string Tag;

        public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            EntityID = stream.ReadVarInt();
            Tag = stream.ReadString();
            return mode;
        }

        public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
        {
            stream.WriteVarInt(EntityID);
            stream.WriteString(Tag);
            return mode;
        }
    }

    #endregion
    /*
        public struct KeepAlivePacket : IPacket
        {
            public KeepAlivePacket()
            {
            }


            public NetworkMode ReadPacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
            {
                return mode;
            }

            public NetworkMode WritePacket(MinecraftStream stream, NetworkMode mode, PacketDirection direction)
            {
                return mode;
            }
        }
    */
}
