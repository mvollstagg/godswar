using System.Buffers.Binary;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.Tests;

public class GamePacketsTests
{
    [Fact]
    public void MsgRoleInfo_HasExact356ByteFramedLayout()
    {
        int[] equips = Enumerable.Repeat(-1, 12).ToArray();
        equips[2] = 2800;

        var msg = new MsgRoleInfo([
            new RoleSummary("Umut", Camp: 0, Profession: 1, Level: 5, Gender: 1, HairStyle: 10, FaceShap: 2, equips),
            new RoleSummary("DigerUmu", Camp: 1, Profession: 0, Level: 3, Gender: 0, HairStyle: 4, FaceShap: 1, Enumerable.Repeat(-1, 12).ToArray()),
        ]);

        byte[] framed = PacketFramer.FramePacket((ushort)GameOpcode.MsgRoleInfo, msg.ToPayload());
        Assert.Equal(356, framed.Length);

        MsgRoleInfo parsed = MsgRoleInfo.Parse(framed.AsSpan(4));
        Assert.Equal(2, parsed.Roles.Count);
        Assert.Equal("Umut", parsed.Roles[0].Name);
        Assert.Equal(5, parsed.Roles[0].Level);
        Assert.Equal(2800, parsed.Roles[0].EquipBaseIds[2]);
        Assert.Equal("DigerUmu", parsed.Roles[1].Name);
        Assert.Equal(1, parsed.Roles[1].Camp);
    }

    [Fact]
    public void MsgSelfInfo_HasExact1192ByteLayoutAndExpectedOffsets()
    {
        var equips = new ItemEquipSlot[12];
        for (int i = 0; i < 12; i++) equips[i] = ItemEquipSlot.Empty;
        equips[2] = new ItemEquipSlot(2800, 0, -1, -1, -1, -1, 1, 1, 1, 1);

        var msg = new MsgSelfInfo(
            DbId: 3795,
            Name: "Umut",
            Gender: 1,
            Camp: 0,
            Belief: 0,
            Profession: 1,
            HairStyle: 10,
            FaceShap: 0,
            MapId: 1,
            EquipMask: 1 << 2,
            UserId: 7,
            X: 98.5f,
            Y: 0f,
            Z: -178.25f,
            MaxHp: 1521,
            MaxMp: 507,
            CurHp: 1521,
            CurMp: 507,
            Exp: 170,
            MaxExp: 1000,
            SkillPoint: 12,
            SkillExp: 24,
            Equips: equips);

        byte[] framed = PacketFramer.FramePacket((ushort)GameOpcode.MsgSelfInfo, msg.ToPayload());
        Assert.Equal(1192, framed.Length);
        Assert.Equal((ushort)GameOpcode.MsgSelfInfo, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(2, 2)));
        Assert.Equal(7u, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(52, 4)));
        Assert.Equal(12u, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(92, 4)));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(100, 4)));
        Assert.Equal(2800, BinaryPrimitives.ReadInt32LittleEndian(framed.AsSpan(104, 4)));
    }

    [Fact]
    public void MsgKitbagPage_HasExact888ByteLayout()
    {
        var slots = new ItemEquipSlot[12];
        for (int i = 0; i < 12; i++) slots[i] = ItemEquipSlot.Empty;
        slots[0] = new ItemEquipSlot(4000, -1, -1, -1, -1, -1, 1, 1, 0, 20);

        var page = new MsgKitbagPage(UserId: 7, BagNum: 1, BagIndex: 0, StartSlot: 0, Flag12: 0x001, slots);
        byte[] framed = PacketFramer.FramePacket((ushort)GameOpcode.MsgKitbag, page.ToPayload());

        Assert.Equal(888, framed.Length);
        Assert.Equal(7u, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(4, 4)));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(20, 4)));
        Assert.Equal(4000, BinaryPrimitives.ReadInt32LittleEndian(framed.AsSpan(24, 4)));
        Assert.Equal(20, framed[24 + 27]);
    }

    [Fact]
    public void MsgSelfPropertyAndSynGameData_Match132And236ByteLayouts()
    {
        var obj = new ClientObjectData(
            Name: "Umut",
            Gender: 1,
            RegionId: 1,
            X: 98f,
            Y: 0f,
            Z: -178f,
            Speed: 4.5f,
            Style: 0,
            Faction: 0,
            Prestige: 100,
            UnionId: 0,
            UnionDuty: 0,
            Knight: 2,
            Contribute: 0,
            StoreNum: 10,
            BagNum: 1,
            Profession: 1,
            Exp: 170,
            Rank: 5,
            Hp: 1521,
            Mp: 507,
            AttackDistance: 2,
            AttackSpeed: 1000,
            HairStyle: 10,
            FaceShap: 0,
            Belief: 0,
            Money: 2100,
            Stone: 5);

        byte[] propFramed = PacketFramer.FramePacket((ushort)GameOpcode.MsgSelfProperty, new MsgSelfProperty(obj).ToPayload());
        Assert.Equal(132, propFramed.Length);
        Assert.Equal(5, propFramed[4 + 92]); // Rank at ObjectData+92 -> wire 96
        Assert.Equal(1521, BinaryPrimitives.ReadInt32LittleEndian(propFramed.AsSpan(4 + 96, 4)));

        byte[] synFramed = PacketFramer.FramePacket((ushort)GameOpcode.MsgSynGameData, new MsgSynGameData(7u, obj, 12u).ToPayload());
        Assert.Equal(236, synFramed.Length);
        Assert.Equal(7u, BinaryPrimitives.ReadUInt32LittleEndian(synFramed.AsSpan(4, 4)));
        Assert.Equal(5, synFramed[8 + 92]); // Rank at wire 100
        Assert.Equal(12u, BinaryPrimitives.ReadUInt32LittleEndian(synFramed.AsSpan(224, 4)));
    }

    [Fact]
    public void MsgObjectInfo_Has628ByteLayoutWithFull64ByteName()
    {
        var obj = new MsgObjectInfo(
            WorldObjectType.Npc,
            Camp: 2,
            MapId: 1,
            UiId: 5007,
            Level: 1,
            Hp: 1000,
            MaxHp: 1000,
            X: 68f,
            Y: 0f,
            Z: -190f,
            Atan2: 1.7f,
            Name: "Athens_Newbie_008_AthenianCivilian1");

        byte[] framed = PacketFramer.FramePacket((ushort)GameOpcode.MsgObjectInfo, obj.ToPayload());
        Assert.Equal(628, framed.Length);
        Assert.Equal((byte)WorldObjectType.Npc, framed[4]);
        Assert.Equal(5007u, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(8, 4)));

        var reader = new PacketReader(framed.AsSpan(44, 64));
        Assert.Equal("Athens_Newbie_008_AthenianCivilian1", reader.ReadFixedString(64));
    }

    [Fact]
    public void MsgWalk_RoundTrips20Bytes()
    {
        var walk = new MsgWalk(IdOrMap: 7, State: 2, X: 102.5f, Z: -175.25f, Atan2: 1.57f);
        byte[] framed = PacketFramer.FramePacket((ushort)GameOpcode.MsgWalk, walk.ToPayload());
        Assert.Equal(20, framed.Length);

        MsgWalk parsed = MsgWalk.Parse(framed.AsSpan(4));
        Assert.Equal(walk, parsed);
    }

    [Fact]
    public void ItemEquipSlot_ParsesLegacyDbString()
    {
        string raw = "[4000,,,,,,0,20,1,1,0]#[]#[2900,50,,,,,1,1,1,1,0]#";
        ItemEquipSlot[] slots = ItemEquipSlot.ParseDbKitbagString(raw, 4);

        Assert.Equal(4000, slots[0].Base);
        Assert.Equal(20, slots[0].Overlap);
        Assert.True(slots[1].IsEmpty);
        Assert.Equal(2900, slots[2].Base);
        Assert.Equal(50, slots[2].Append0);
        Assert.Equal(1, slots[2].Binding);
        Assert.True(slots[3].IsEmpty);
    }

    [Fact]
    public void MsgRoleInfo_ToV227Payload_Has496ByteLayout()
    {
        var roles = new List<RoleSummary>
        {
            new("Hero", 0, 1, 5, 1, 2, 3, [1001, -1]),
            new("Mage", 1, 3, 10, 0, 4, 1, [-1, -1]),
        };

        var msg = new MsgRoleInfo(roles);
        byte[] payload = msg.ToV227Payload();

        Assert.Equal(MsgRoleInfo.V227PayloadLength, payload.Length);
        Assert.Equal((byte)2, payload[0]);
        Assert.Equal("Hero", System.Text.Encoding.ASCII.GetString(payload, 1, 4));
        Assert.Equal(1001, BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(40, 4)));
        Assert.Equal("Mage", System.Text.Encoding.ASCII.GetString(payload, 124, 4));
        Assert.Equal((byte)10, payload[124 + 34]);
    }
}
