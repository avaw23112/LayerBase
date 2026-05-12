using LayerBase;
using LayerBase.Core.Event;
using LayerBase.DI;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using IServiceCollection = LayerBase.DI.IServiceCollection;
using IServiceProvider = System.IServiceProvider;

namespace LayerBaseCompareBenchmarks;

public interface IManyNotifyEventPayload
{
    int Value { get; }
}

public readonly struct ManyNotifyEvent_000 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_000(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_000 Instance = new(1);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_001 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_001(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_001 Instance = new(2);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_002 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_002(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_002 Instance = new(3);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_003 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_003(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_003 Instance = new(4);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_004 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_004(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_004 Instance = new(5);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_005 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_005(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_005 Instance = new(6);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_006 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_006(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_006 Instance = new(7);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_007 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_007(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_007 Instance = new(8);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_008 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_008(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_008 Instance = new(9);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_009 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_009(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_009 Instance = new(10);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_010 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_010(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_010 Instance = new(11);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_011 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_011(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_011 Instance = new(12);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_012 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_012(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_012 Instance = new(13);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_013 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_013(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_013 Instance = new(14);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_014 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_014(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_014 Instance = new(15);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_015 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_015(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_015 Instance = new(16);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_016 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_016(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_016 Instance = new(17);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_017 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_017(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_017 Instance = new(18);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_018 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_018(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_018 Instance = new(19);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_019 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_019(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_019 Instance = new(20);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_020 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_020(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_020 Instance = new(21);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_021 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_021(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_021 Instance = new(22);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_022 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_022(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_022 Instance = new(23);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_023 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_023(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_023 Instance = new(24);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_024 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_024(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_024 Instance = new(25);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_025 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_025(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_025 Instance = new(26);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_026 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_026(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_026 Instance = new(27);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_027 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_027(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_027 Instance = new(28);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_028 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_028(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_028 Instance = new(29);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_029 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_029(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_029 Instance = new(30);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_030 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_030(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_030 Instance = new(31);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_031 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_031(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_031 Instance = new(32);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_032 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_032(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_032 Instance = new(33);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_033 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_033(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_033 Instance = new(34);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_034 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_034(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_034 Instance = new(35);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_035 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_035(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_035 Instance = new(36);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_036 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_036(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_036 Instance = new(37);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_037 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_037(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_037 Instance = new(38);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_038 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_038(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_038 Instance = new(39);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_039 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_039(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_039 Instance = new(40);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_040 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_040(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_040 Instance = new(41);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_041 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_041(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_041 Instance = new(42);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_042 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_042(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_042 Instance = new(43);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_043 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_043(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_043 Instance = new(44);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_044 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_044(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_044 Instance = new(45);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_045 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_045(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_045 Instance = new(46);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_046 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_046(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_046 Instance = new(47);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_047 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_047(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_047 Instance = new(48);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_048 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_048(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_048 Instance = new(49);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_049 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_049(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_049 Instance = new(50);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_050 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_050(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_050 Instance = new(51);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_051 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_051(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_051 Instance = new(52);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_052 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_052(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_052 Instance = new(53);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_053 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_053(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_053 Instance = new(54);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_054 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_054(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_054 Instance = new(55);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_055 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_055(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_055 Instance = new(56);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_056 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_056(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_056 Instance = new(57);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_057 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_057(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_057 Instance = new(58);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_058 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_058(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_058 Instance = new(59);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_059 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_059(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_059 Instance = new(60);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_060 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_060(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_060 Instance = new(61);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_061 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_061(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_061 Instance = new(62);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_062 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_062(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_062 Instance = new(63);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_063 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_063(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_063 Instance = new(64);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_064 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_064(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_064 Instance = new(65);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_065 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_065(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_065 Instance = new(66);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_066 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_066(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_066 Instance = new(67);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_067 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_067(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_067 Instance = new(68);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_068 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_068(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_068 Instance = new(69);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_069 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_069(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_069 Instance = new(70);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_070 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_070(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_070 Instance = new(71);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_071 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_071(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_071 Instance = new(72);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_072 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_072(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_072 Instance = new(73);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_073 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_073(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_073 Instance = new(74);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_074 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_074(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_074 Instance = new(75);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_075 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_075(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_075 Instance = new(76);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_076 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_076(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_076 Instance = new(77);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_077 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_077(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_077 Instance = new(78);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_078 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_078(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_078 Instance = new(79);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_079 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_079(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_079 Instance = new(80);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_080 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_080(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_080 Instance = new(81);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_081 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_081(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_081 Instance = new(82);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_082 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_082(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_082 Instance = new(83);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_083 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_083(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_083 Instance = new(84);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_084 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_084(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_084 Instance = new(85);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_085 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_085(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_085 Instance = new(86);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_086 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_086(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_086 Instance = new(87);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_087 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_087(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_087 Instance = new(88);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_088 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_088(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_088 Instance = new(89);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_089 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_089(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_089 Instance = new(90);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_090 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_090(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_090 Instance = new(91);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_091 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_091(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_091 Instance = new(92);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_092 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_092(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_092 Instance = new(93);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_093 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_093(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_093 Instance = new(94);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_094 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_094(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_094 Instance = new(95);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_095 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_095(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_095 Instance = new(96);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_096 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_096(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_096 Instance = new(97);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_097 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_097(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_097 Instance = new(98);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_098 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_098(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_098 Instance = new(99);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_099 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_099(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_099 Instance = new(100);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_100 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_100(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_100 Instance = new(101);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_101 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_101(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_101 Instance = new(102);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_102 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_102(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_102 Instance = new(103);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_103 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_103(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_103 Instance = new(104);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_104 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_104(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_104 Instance = new(105);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_105 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_105(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_105 Instance = new(106);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_106 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_106(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_106 Instance = new(107);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_107 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_107(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_107 Instance = new(108);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_108 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_108(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_108 Instance = new(109);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_109 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_109(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_109 Instance = new(110);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_110 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_110(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_110 Instance = new(111);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_111 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_111(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_111 Instance = new(112);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_112 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_112(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_112 Instance = new(113);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_113 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_113(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_113 Instance = new(114);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_114 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_114(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_114 Instance = new(115);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_115 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_115(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_115 Instance = new(116);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_116 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_116(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_116 Instance = new(117);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_117 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_117(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_117 Instance = new(118);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_118 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_118(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_118 Instance = new(119);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_119 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_119(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_119 Instance = new(120);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_120 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_120(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_120 Instance = new(121);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_121 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_121(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_121 Instance = new(122);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_122 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_122(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_122 Instance = new(123);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_123 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_123(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_123 Instance = new(124);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_124 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_124(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_124 Instance = new(125);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_125 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_125(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_125 Instance = new(126);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_126 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_126(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_126 Instance = new(127);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_127 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_127(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_127 Instance = new(128);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_128 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_128(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_128 Instance = new(129);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_129 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_129(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_129 Instance = new(130);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_130 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_130(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_130 Instance = new(131);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_131 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_131(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_131 Instance = new(132);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_132 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_132(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_132 Instance = new(133);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_133 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_133(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_133 Instance = new(134);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_134 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_134(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_134 Instance = new(135);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_135 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_135(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_135 Instance = new(136);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_136 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_136(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_136 Instance = new(137);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_137 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_137(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_137 Instance = new(138);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_138 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_138(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_138 Instance = new(139);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_139 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_139(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_139 Instance = new(140);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_140 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_140(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_140 Instance = new(141);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_141 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_141(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_141 Instance = new(142);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_142 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_142(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_142 Instance = new(143);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_143 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_143(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_143 Instance = new(144);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_144 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_144(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_144 Instance = new(145);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_145 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_145(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_145 Instance = new(146);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_146 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_146(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_146 Instance = new(147);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_147 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_147(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_147 Instance = new(148);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_148 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_148(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_148 Instance = new(149);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_149 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_149(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_149 Instance = new(150);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_150 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_150(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_150 Instance = new(151);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_151 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_151(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_151 Instance = new(152);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_152 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_152(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_152 Instance = new(153);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_153 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_153(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_153 Instance = new(154);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_154 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_154(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_154 Instance = new(155);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_155 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_155(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_155 Instance = new(156);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_156 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_156(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_156 Instance = new(157);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_157 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_157(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_157 Instance = new(158);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_158 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_158(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_158 Instance = new(159);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_159 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_159(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_159 Instance = new(160);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_160 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_160(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_160 Instance = new(161);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_161 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_161(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_161 Instance = new(162);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_162 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_162(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_162 Instance = new(163);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_163 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_163(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_163 Instance = new(164);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_164 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_164(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_164 Instance = new(165);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_165 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_165(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_165 Instance = new(166);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_166 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_166(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_166 Instance = new(167);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_167 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_167(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_167 Instance = new(168);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_168 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_168(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_168 Instance = new(169);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_169 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_169(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_169 Instance = new(170);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_170 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_170(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_170 Instance = new(171);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_171 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_171(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_171 Instance = new(172);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_172 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_172(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_172 Instance = new(173);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_173 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_173(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_173 Instance = new(174);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_174 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_174(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_174 Instance = new(175);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_175 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_175(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_175 Instance = new(176);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_176 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_176(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_176 Instance = new(177);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_177 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_177(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_177 Instance = new(178);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_178 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_178(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_178 Instance = new(179);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_179 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_179(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_179 Instance = new(180);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_180 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_180(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_180 Instance = new(181);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_181 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_181(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_181 Instance = new(182);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_182 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_182(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_182 Instance = new(183);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_183 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_183(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_183 Instance = new(184);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_184 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_184(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_184 Instance = new(185);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_185 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_185(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_185 Instance = new(186);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_186 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_186(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_186 Instance = new(187);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_187 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_187(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_187 Instance = new(188);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_188 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_188(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_188 Instance = new(189);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_189 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_189(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_189 Instance = new(190);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_190 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_190(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_190 Instance = new(191);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_191 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_191(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_191 Instance = new(192);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_192 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_192(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_192 Instance = new(193);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_193 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_193(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_193 Instance = new(194);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_194 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_194(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_194 Instance = new(195);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_195 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_195(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_195 Instance = new(196);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_196 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_196(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_196 Instance = new(197);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_197 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_197(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_197 Instance = new(198);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_198 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_198(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_198 Instance = new(199);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_199 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_199(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_199 Instance = new(200);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_200 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_200(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_200 Instance = new(201);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_201 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_201(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_201 Instance = new(202);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_202 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_202(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_202 Instance = new(203);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_203 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_203(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_203 Instance = new(204);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_204 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_204(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_204 Instance = new(205);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_205 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_205(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_205 Instance = new(206);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_206 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_206(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_206 Instance = new(207);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_207 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_207(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_207 Instance = new(208);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_208 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_208(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_208 Instance = new(209);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_209 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_209(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_209 Instance = new(210);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_210 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_210(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_210 Instance = new(211);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_211 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_211(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_211 Instance = new(212);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_212 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_212(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_212 Instance = new(213);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_213 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_213(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_213 Instance = new(214);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_214 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_214(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_214 Instance = new(215);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_215 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_215(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_215 Instance = new(216);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_216 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_216(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_216 Instance = new(217);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_217 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_217(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_217 Instance = new(218);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_218 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_218(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_218 Instance = new(219);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_219 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_219(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_219 Instance = new(220);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_220 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_220(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_220 Instance = new(221);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_221 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_221(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_221 Instance = new(222);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_222 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_222(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_222 Instance = new(223);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_223 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_223(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_223 Instance = new(224);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_224 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_224(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_224 Instance = new(225);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_225 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_225(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_225 Instance = new(226);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_226 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_226(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_226 Instance = new(227);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_227 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_227(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_227 Instance = new(228);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_228 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_228(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_228 Instance = new(229);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_229 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_229(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_229 Instance = new(230);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_230 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_230(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_230 Instance = new(231);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_231 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_231(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_231 Instance = new(232);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_232 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_232(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_232 Instance = new(233);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_233 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_233(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_233 Instance = new(234);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_234 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_234(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_234 Instance = new(235);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_235 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_235(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_235 Instance = new(236);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_236 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_236(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_236 Instance = new(237);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_237 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_237(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_237 Instance = new(238);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_238 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_238(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_238 Instance = new(239);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_239 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_239(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_239 Instance = new(240);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_240 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_240(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_240 Instance = new(241);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_241 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_241(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_241 Instance = new(242);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_242 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_242(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_242 Instance = new(243);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_243 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_243(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_243 Instance = new(244);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_244 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_244(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_244 Instance = new(245);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_245 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_245(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_245 Instance = new(246);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_246 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_246(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_246 Instance = new(247);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_247 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_247(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_247 Instance = new(248);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_248 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_248(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_248 Instance = new(249);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_249 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_249(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_249 Instance = new(250);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_250 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_250(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_250 Instance = new(251);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_251 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_251(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_251 Instance = new(252);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_252 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_252(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_252 Instance = new(253);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_253 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_253(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_253 Instance = new(254);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_254 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_254(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_254 Instance = new(255);
    public int Value { get; }
}

public readonly struct ManyNotifyEvent_255 : IManyNotifyEventPayload
{
    public ManyNotifyEvent_255(int value)
    {
        Value = value;
    }

    public static readonly ManyNotifyEvent_255 Instance = new(256);
    public int Value { get; }
}

internal sealed class ManyNotifyBatch32Publishers
{
    public IPublisher<ManyNotifyEvent_000> P000 = null!;
    public IPublisher<ManyNotifyEvent_001> P001 = null!;
    public IPublisher<ManyNotifyEvent_002> P002 = null!;
    public IPublisher<ManyNotifyEvent_003> P003 = null!;
    public IPublisher<ManyNotifyEvent_004> P004 = null!;
    public IPublisher<ManyNotifyEvent_005> P005 = null!;
    public IPublisher<ManyNotifyEvent_006> P006 = null!;
    public IPublisher<ManyNotifyEvent_007> P007 = null!;
    public IPublisher<ManyNotifyEvent_008> P008 = null!;
    public IPublisher<ManyNotifyEvent_009> P009 = null!;
    public IPublisher<ManyNotifyEvent_010> P010 = null!;
    public IPublisher<ManyNotifyEvent_011> P011 = null!;
    public IPublisher<ManyNotifyEvent_012> P012 = null!;
    public IPublisher<ManyNotifyEvent_013> P013 = null!;
    public IPublisher<ManyNotifyEvent_014> P014 = null!;
    public IPublisher<ManyNotifyEvent_015> P015 = null!;
    public IPublisher<ManyNotifyEvent_016> P016 = null!;
    public IPublisher<ManyNotifyEvent_017> P017 = null!;
    public IPublisher<ManyNotifyEvent_018> P018 = null!;
    public IPublisher<ManyNotifyEvent_019> P019 = null!;
    public IPublisher<ManyNotifyEvent_020> P020 = null!;
    public IPublisher<ManyNotifyEvent_021> P021 = null!;
    public IPublisher<ManyNotifyEvent_022> P022 = null!;
    public IPublisher<ManyNotifyEvent_023> P023 = null!;
    public IPublisher<ManyNotifyEvent_024> P024 = null!;
    public IPublisher<ManyNotifyEvent_025> P025 = null!;
    public IPublisher<ManyNotifyEvent_026> P026 = null!;
    public IPublisher<ManyNotifyEvent_027> P027 = null!;
    public IPublisher<ManyNotifyEvent_028> P028 = null!;
    public IPublisher<ManyNotifyEvent_029> P029 = null!;
    public IPublisher<ManyNotifyEvent_030> P030 = null!;
    public IPublisher<ManyNotifyEvent_031> P031 = null!;
}

internal sealed class ManyNotifyBatch128Publishers
{
    public IPublisher<ManyNotifyEvent_000> P000 = null!;
    public IPublisher<ManyNotifyEvent_001> P001 = null!;
    public IPublisher<ManyNotifyEvent_002> P002 = null!;
    public IPublisher<ManyNotifyEvent_003> P003 = null!;
    public IPublisher<ManyNotifyEvent_004> P004 = null!;
    public IPublisher<ManyNotifyEvent_005> P005 = null!;
    public IPublisher<ManyNotifyEvent_006> P006 = null!;
    public IPublisher<ManyNotifyEvent_007> P007 = null!;
    public IPublisher<ManyNotifyEvent_008> P008 = null!;
    public IPublisher<ManyNotifyEvent_009> P009 = null!;
    public IPublisher<ManyNotifyEvent_010> P010 = null!;
    public IPublisher<ManyNotifyEvent_011> P011 = null!;
    public IPublisher<ManyNotifyEvent_012> P012 = null!;
    public IPublisher<ManyNotifyEvent_013> P013 = null!;
    public IPublisher<ManyNotifyEvent_014> P014 = null!;
    public IPublisher<ManyNotifyEvent_015> P015 = null!;
    public IPublisher<ManyNotifyEvent_016> P016 = null!;
    public IPublisher<ManyNotifyEvent_017> P017 = null!;
    public IPublisher<ManyNotifyEvent_018> P018 = null!;
    public IPublisher<ManyNotifyEvent_019> P019 = null!;
    public IPublisher<ManyNotifyEvent_020> P020 = null!;
    public IPublisher<ManyNotifyEvent_021> P021 = null!;
    public IPublisher<ManyNotifyEvent_022> P022 = null!;
    public IPublisher<ManyNotifyEvent_023> P023 = null!;
    public IPublisher<ManyNotifyEvent_024> P024 = null!;
    public IPublisher<ManyNotifyEvent_025> P025 = null!;
    public IPublisher<ManyNotifyEvent_026> P026 = null!;
    public IPublisher<ManyNotifyEvent_027> P027 = null!;
    public IPublisher<ManyNotifyEvent_028> P028 = null!;
    public IPublisher<ManyNotifyEvent_029> P029 = null!;
    public IPublisher<ManyNotifyEvent_030> P030 = null!;
    public IPublisher<ManyNotifyEvent_031> P031 = null!;
    public IPublisher<ManyNotifyEvent_032> P032 = null!;
    public IPublisher<ManyNotifyEvent_033> P033 = null!;
    public IPublisher<ManyNotifyEvent_034> P034 = null!;
    public IPublisher<ManyNotifyEvent_035> P035 = null!;
    public IPublisher<ManyNotifyEvent_036> P036 = null!;
    public IPublisher<ManyNotifyEvent_037> P037 = null!;
    public IPublisher<ManyNotifyEvent_038> P038 = null!;
    public IPublisher<ManyNotifyEvent_039> P039 = null!;
    public IPublisher<ManyNotifyEvent_040> P040 = null!;
    public IPublisher<ManyNotifyEvent_041> P041 = null!;
    public IPublisher<ManyNotifyEvent_042> P042 = null!;
    public IPublisher<ManyNotifyEvent_043> P043 = null!;
    public IPublisher<ManyNotifyEvent_044> P044 = null!;
    public IPublisher<ManyNotifyEvent_045> P045 = null!;
    public IPublisher<ManyNotifyEvent_046> P046 = null!;
    public IPublisher<ManyNotifyEvent_047> P047 = null!;
    public IPublisher<ManyNotifyEvent_048> P048 = null!;
    public IPublisher<ManyNotifyEvent_049> P049 = null!;
    public IPublisher<ManyNotifyEvent_050> P050 = null!;
    public IPublisher<ManyNotifyEvent_051> P051 = null!;
    public IPublisher<ManyNotifyEvent_052> P052 = null!;
    public IPublisher<ManyNotifyEvent_053> P053 = null!;
    public IPublisher<ManyNotifyEvent_054> P054 = null!;
    public IPublisher<ManyNotifyEvent_055> P055 = null!;
    public IPublisher<ManyNotifyEvent_056> P056 = null!;
    public IPublisher<ManyNotifyEvent_057> P057 = null!;
    public IPublisher<ManyNotifyEvent_058> P058 = null!;
    public IPublisher<ManyNotifyEvent_059> P059 = null!;
    public IPublisher<ManyNotifyEvent_060> P060 = null!;
    public IPublisher<ManyNotifyEvent_061> P061 = null!;
    public IPublisher<ManyNotifyEvent_062> P062 = null!;
    public IPublisher<ManyNotifyEvent_063> P063 = null!;
    public IPublisher<ManyNotifyEvent_064> P064 = null!;
    public IPublisher<ManyNotifyEvent_065> P065 = null!;
    public IPublisher<ManyNotifyEvent_066> P066 = null!;
    public IPublisher<ManyNotifyEvent_067> P067 = null!;
    public IPublisher<ManyNotifyEvent_068> P068 = null!;
    public IPublisher<ManyNotifyEvent_069> P069 = null!;
    public IPublisher<ManyNotifyEvent_070> P070 = null!;
    public IPublisher<ManyNotifyEvent_071> P071 = null!;
    public IPublisher<ManyNotifyEvent_072> P072 = null!;
    public IPublisher<ManyNotifyEvent_073> P073 = null!;
    public IPublisher<ManyNotifyEvent_074> P074 = null!;
    public IPublisher<ManyNotifyEvent_075> P075 = null!;
    public IPublisher<ManyNotifyEvent_076> P076 = null!;
    public IPublisher<ManyNotifyEvent_077> P077 = null!;
    public IPublisher<ManyNotifyEvent_078> P078 = null!;
    public IPublisher<ManyNotifyEvent_079> P079 = null!;
    public IPublisher<ManyNotifyEvent_080> P080 = null!;
    public IPublisher<ManyNotifyEvent_081> P081 = null!;
    public IPublisher<ManyNotifyEvent_082> P082 = null!;
    public IPublisher<ManyNotifyEvent_083> P083 = null!;
    public IPublisher<ManyNotifyEvent_084> P084 = null!;
    public IPublisher<ManyNotifyEvent_085> P085 = null!;
    public IPublisher<ManyNotifyEvent_086> P086 = null!;
    public IPublisher<ManyNotifyEvent_087> P087 = null!;
    public IPublisher<ManyNotifyEvent_088> P088 = null!;
    public IPublisher<ManyNotifyEvent_089> P089 = null!;
    public IPublisher<ManyNotifyEvent_090> P090 = null!;
    public IPublisher<ManyNotifyEvent_091> P091 = null!;
    public IPublisher<ManyNotifyEvent_092> P092 = null!;
    public IPublisher<ManyNotifyEvent_093> P093 = null!;
    public IPublisher<ManyNotifyEvent_094> P094 = null!;
    public IPublisher<ManyNotifyEvent_095> P095 = null!;
    public IPublisher<ManyNotifyEvent_096> P096 = null!;
    public IPublisher<ManyNotifyEvent_097> P097 = null!;
    public IPublisher<ManyNotifyEvent_098> P098 = null!;
    public IPublisher<ManyNotifyEvent_099> P099 = null!;
    public IPublisher<ManyNotifyEvent_100> P100 = null!;
    public IPublisher<ManyNotifyEvent_101> P101 = null!;
    public IPublisher<ManyNotifyEvent_102> P102 = null!;
    public IPublisher<ManyNotifyEvent_103> P103 = null!;
    public IPublisher<ManyNotifyEvent_104> P104 = null!;
    public IPublisher<ManyNotifyEvent_105> P105 = null!;
    public IPublisher<ManyNotifyEvent_106> P106 = null!;
    public IPublisher<ManyNotifyEvent_107> P107 = null!;
    public IPublisher<ManyNotifyEvent_108> P108 = null!;
    public IPublisher<ManyNotifyEvent_109> P109 = null!;
    public IPublisher<ManyNotifyEvent_110> P110 = null!;
    public IPublisher<ManyNotifyEvent_111> P111 = null!;
    public IPublisher<ManyNotifyEvent_112> P112 = null!;
    public IPublisher<ManyNotifyEvent_113> P113 = null!;
    public IPublisher<ManyNotifyEvent_114> P114 = null!;
    public IPublisher<ManyNotifyEvent_115> P115 = null!;
    public IPublisher<ManyNotifyEvent_116> P116 = null!;
    public IPublisher<ManyNotifyEvent_117> P117 = null!;
    public IPublisher<ManyNotifyEvent_118> P118 = null!;
    public IPublisher<ManyNotifyEvent_119> P119 = null!;
    public IPublisher<ManyNotifyEvent_120> P120 = null!;
    public IPublisher<ManyNotifyEvent_121> P121 = null!;
    public IPublisher<ManyNotifyEvent_122> P122 = null!;
    public IPublisher<ManyNotifyEvent_123> P123 = null!;
    public IPublisher<ManyNotifyEvent_124> P124 = null!;
    public IPublisher<ManyNotifyEvent_125> P125 = null!;
    public IPublisher<ManyNotifyEvent_126> P126 = null!;
    public IPublisher<ManyNotifyEvent_127> P127 = null!;
}

internal sealed class ManyNotifyBatch256Publishers
{
    public IPublisher<ManyNotifyEvent_000> P000 = null!;
    public IPublisher<ManyNotifyEvent_001> P001 = null!;
    public IPublisher<ManyNotifyEvent_002> P002 = null!;
    public IPublisher<ManyNotifyEvent_003> P003 = null!;
    public IPublisher<ManyNotifyEvent_004> P004 = null!;
    public IPublisher<ManyNotifyEvent_005> P005 = null!;
    public IPublisher<ManyNotifyEvent_006> P006 = null!;
    public IPublisher<ManyNotifyEvent_007> P007 = null!;
    public IPublisher<ManyNotifyEvent_008> P008 = null!;
    public IPublisher<ManyNotifyEvent_009> P009 = null!;
    public IPublisher<ManyNotifyEvent_010> P010 = null!;
    public IPublisher<ManyNotifyEvent_011> P011 = null!;
    public IPublisher<ManyNotifyEvent_012> P012 = null!;
    public IPublisher<ManyNotifyEvent_013> P013 = null!;
    public IPublisher<ManyNotifyEvent_014> P014 = null!;
    public IPublisher<ManyNotifyEvent_015> P015 = null!;
    public IPublisher<ManyNotifyEvent_016> P016 = null!;
    public IPublisher<ManyNotifyEvent_017> P017 = null!;
    public IPublisher<ManyNotifyEvent_018> P018 = null!;
    public IPublisher<ManyNotifyEvent_019> P019 = null!;
    public IPublisher<ManyNotifyEvent_020> P020 = null!;
    public IPublisher<ManyNotifyEvent_021> P021 = null!;
    public IPublisher<ManyNotifyEvent_022> P022 = null!;
    public IPublisher<ManyNotifyEvent_023> P023 = null!;
    public IPublisher<ManyNotifyEvent_024> P024 = null!;
    public IPublisher<ManyNotifyEvent_025> P025 = null!;
    public IPublisher<ManyNotifyEvent_026> P026 = null!;
    public IPublisher<ManyNotifyEvent_027> P027 = null!;
    public IPublisher<ManyNotifyEvent_028> P028 = null!;
    public IPublisher<ManyNotifyEvent_029> P029 = null!;
    public IPublisher<ManyNotifyEvent_030> P030 = null!;
    public IPublisher<ManyNotifyEvent_031> P031 = null!;
    public IPublisher<ManyNotifyEvent_032> P032 = null!;
    public IPublisher<ManyNotifyEvent_033> P033 = null!;
    public IPublisher<ManyNotifyEvent_034> P034 = null!;
    public IPublisher<ManyNotifyEvent_035> P035 = null!;
    public IPublisher<ManyNotifyEvent_036> P036 = null!;
    public IPublisher<ManyNotifyEvent_037> P037 = null!;
    public IPublisher<ManyNotifyEvent_038> P038 = null!;
    public IPublisher<ManyNotifyEvent_039> P039 = null!;
    public IPublisher<ManyNotifyEvent_040> P040 = null!;
    public IPublisher<ManyNotifyEvent_041> P041 = null!;
    public IPublisher<ManyNotifyEvent_042> P042 = null!;
    public IPublisher<ManyNotifyEvent_043> P043 = null!;
    public IPublisher<ManyNotifyEvent_044> P044 = null!;
    public IPublisher<ManyNotifyEvent_045> P045 = null!;
    public IPublisher<ManyNotifyEvent_046> P046 = null!;
    public IPublisher<ManyNotifyEvent_047> P047 = null!;
    public IPublisher<ManyNotifyEvent_048> P048 = null!;
    public IPublisher<ManyNotifyEvent_049> P049 = null!;
    public IPublisher<ManyNotifyEvent_050> P050 = null!;
    public IPublisher<ManyNotifyEvent_051> P051 = null!;
    public IPublisher<ManyNotifyEvent_052> P052 = null!;
    public IPublisher<ManyNotifyEvent_053> P053 = null!;
    public IPublisher<ManyNotifyEvent_054> P054 = null!;
    public IPublisher<ManyNotifyEvent_055> P055 = null!;
    public IPublisher<ManyNotifyEvent_056> P056 = null!;
    public IPublisher<ManyNotifyEvent_057> P057 = null!;
    public IPublisher<ManyNotifyEvent_058> P058 = null!;
    public IPublisher<ManyNotifyEvent_059> P059 = null!;
    public IPublisher<ManyNotifyEvent_060> P060 = null!;
    public IPublisher<ManyNotifyEvent_061> P061 = null!;
    public IPublisher<ManyNotifyEvent_062> P062 = null!;
    public IPublisher<ManyNotifyEvent_063> P063 = null!;
    public IPublisher<ManyNotifyEvent_064> P064 = null!;
    public IPublisher<ManyNotifyEvent_065> P065 = null!;
    public IPublisher<ManyNotifyEvent_066> P066 = null!;
    public IPublisher<ManyNotifyEvent_067> P067 = null!;
    public IPublisher<ManyNotifyEvent_068> P068 = null!;
    public IPublisher<ManyNotifyEvent_069> P069 = null!;
    public IPublisher<ManyNotifyEvent_070> P070 = null!;
    public IPublisher<ManyNotifyEvent_071> P071 = null!;
    public IPublisher<ManyNotifyEvent_072> P072 = null!;
    public IPublisher<ManyNotifyEvent_073> P073 = null!;
    public IPublisher<ManyNotifyEvent_074> P074 = null!;
    public IPublisher<ManyNotifyEvent_075> P075 = null!;
    public IPublisher<ManyNotifyEvent_076> P076 = null!;
    public IPublisher<ManyNotifyEvent_077> P077 = null!;
    public IPublisher<ManyNotifyEvent_078> P078 = null!;
    public IPublisher<ManyNotifyEvent_079> P079 = null!;
    public IPublisher<ManyNotifyEvent_080> P080 = null!;
    public IPublisher<ManyNotifyEvent_081> P081 = null!;
    public IPublisher<ManyNotifyEvent_082> P082 = null!;
    public IPublisher<ManyNotifyEvent_083> P083 = null!;
    public IPublisher<ManyNotifyEvent_084> P084 = null!;
    public IPublisher<ManyNotifyEvent_085> P085 = null!;
    public IPublisher<ManyNotifyEvent_086> P086 = null!;
    public IPublisher<ManyNotifyEvent_087> P087 = null!;
    public IPublisher<ManyNotifyEvent_088> P088 = null!;
    public IPublisher<ManyNotifyEvent_089> P089 = null!;
    public IPublisher<ManyNotifyEvent_090> P090 = null!;
    public IPublisher<ManyNotifyEvent_091> P091 = null!;
    public IPublisher<ManyNotifyEvent_092> P092 = null!;
    public IPublisher<ManyNotifyEvent_093> P093 = null!;
    public IPublisher<ManyNotifyEvent_094> P094 = null!;
    public IPublisher<ManyNotifyEvent_095> P095 = null!;
    public IPublisher<ManyNotifyEvent_096> P096 = null!;
    public IPublisher<ManyNotifyEvent_097> P097 = null!;
    public IPublisher<ManyNotifyEvent_098> P098 = null!;
    public IPublisher<ManyNotifyEvent_099> P099 = null!;
    public IPublisher<ManyNotifyEvent_100> P100 = null!;
    public IPublisher<ManyNotifyEvent_101> P101 = null!;
    public IPublisher<ManyNotifyEvent_102> P102 = null!;
    public IPublisher<ManyNotifyEvent_103> P103 = null!;
    public IPublisher<ManyNotifyEvent_104> P104 = null!;
    public IPublisher<ManyNotifyEvent_105> P105 = null!;
    public IPublisher<ManyNotifyEvent_106> P106 = null!;
    public IPublisher<ManyNotifyEvent_107> P107 = null!;
    public IPublisher<ManyNotifyEvent_108> P108 = null!;
    public IPublisher<ManyNotifyEvent_109> P109 = null!;
    public IPublisher<ManyNotifyEvent_110> P110 = null!;
    public IPublisher<ManyNotifyEvent_111> P111 = null!;
    public IPublisher<ManyNotifyEvent_112> P112 = null!;
    public IPublisher<ManyNotifyEvent_113> P113 = null!;
    public IPublisher<ManyNotifyEvent_114> P114 = null!;
    public IPublisher<ManyNotifyEvent_115> P115 = null!;
    public IPublisher<ManyNotifyEvent_116> P116 = null!;
    public IPublisher<ManyNotifyEvent_117> P117 = null!;
    public IPublisher<ManyNotifyEvent_118> P118 = null!;
    public IPublisher<ManyNotifyEvent_119> P119 = null!;
    public IPublisher<ManyNotifyEvent_120> P120 = null!;
    public IPublisher<ManyNotifyEvent_121> P121 = null!;
    public IPublisher<ManyNotifyEvent_122> P122 = null!;
    public IPublisher<ManyNotifyEvent_123> P123 = null!;
    public IPublisher<ManyNotifyEvent_124> P124 = null!;
    public IPublisher<ManyNotifyEvent_125> P125 = null!;
    public IPublisher<ManyNotifyEvent_126> P126 = null!;
    public IPublisher<ManyNotifyEvent_127> P127 = null!;
    public IPublisher<ManyNotifyEvent_128> P128 = null!;
    public IPublisher<ManyNotifyEvent_129> P129 = null!;
    public IPublisher<ManyNotifyEvent_130> P130 = null!;
    public IPublisher<ManyNotifyEvent_131> P131 = null!;
    public IPublisher<ManyNotifyEvent_132> P132 = null!;
    public IPublisher<ManyNotifyEvent_133> P133 = null!;
    public IPublisher<ManyNotifyEvent_134> P134 = null!;
    public IPublisher<ManyNotifyEvent_135> P135 = null!;
    public IPublisher<ManyNotifyEvent_136> P136 = null!;
    public IPublisher<ManyNotifyEvent_137> P137 = null!;
    public IPublisher<ManyNotifyEvent_138> P138 = null!;
    public IPublisher<ManyNotifyEvent_139> P139 = null!;
    public IPublisher<ManyNotifyEvent_140> P140 = null!;
    public IPublisher<ManyNotifyEvent_141> P141 = null!;
    public IPublisher<ManyNotifyEvent_142> P142 = null!;
    public IPublisher<ManyNotifyEvent_143> P143 = null!;
    public IPublisher<ManyNotifyEvent_144> P144 = null!;
    public IPublisher<ManyNotifyEvent_145> P145 = null!;
    public IPublisher<ManyNotifyEvent_146> P146 = null!;
    public IPublisher<ManyNotifyEvent_147> P147 = null!;
    public IPublisher<ManyNotifyEvent_148> P148 = null!;
    public IPublisher<ManyNotifyEvent_149> P149 = null!;
    public IPublisher<ManyNotifyEvent_150> P150 = null!;
    public IPublisher<ManyNotifyEvent_151> P151 = null!;
    public IPublisher<ManyNotifyEvent_152> P152 = null!;
    public IPublisher<ManyNotifyEvent_153> P153 = null!;
    public IPublisher<ManyNotifyEvent_154> P154 = null!;
    public IPublisher<ManyNotifyEvent_155> P155 = null!;
    public IPublisher<ManyNotifyEvent_156> P156 = null!;
    public IPublisher<ManyNotifyEvent_157> P157 = null!;
    public IPublisher<ManyNotifyEvent_158> P158 = null!;
    public IPublisher<ManyNotifyEvent_159> P159 = null!;
    public IPublisher<ManyNotifyEvent_160> P160 = null!;
    public IPublisher<ManyNotifyEvent_161> P161 = null!;
    public IPublisher<ManyNotifyEvent_162> P162 = null!;
    public IPublisher<ManyNotifyEvent_163> P163 = null!;
    public IPublisher<ManyNotifyEvent_164> P164 = null!;
    public IPublisher<ManyNotifyEvent_165> P165 = null!;
    public IPublisher<ManyNotifyEvent_166> P166 = null!;
    public IPublisher<ManyNotifyEvent_167> P167 = null!;
    public IPublisher<ManyNotifyEvent_168> P168 = null!;
    public IPublisher<ManyNotifyEvent_169> P169 = null!;
    public IPublisher<ManyNotifyEvent_170> P170 = null!;
    public IPublisher<ManyNotifyEvent_171> P171 = null!;
    public IPublisher<ManyNotifyEvent_172> P172 = null!;
    public IPublisher<ManyNotifyEvent_173> P173 = null!;
    public IPublisher<ManyNotifyEvent_174> P174 = null!;
    public IPublisher<ManyNotifyEvent_175> P175 = null!;
    public IPublisher<ManyNotifyEvent_176> P176 = null!;
    public IPublisher<ManyNotifyEvent_177> P177 = null!;
    public IPublisher<ManyNotifyEvent_178> P178 = null!;
    public IPublisher<ManyNotifyEvent_179> P179 = null!;
    public IPublisher<ManyNotifyEvent_180> P180 = null!;
    public IPublisher<ManyNotifyEvent_181> P181 = null!;
    public IPublisher<ManyNotifyEvent_182> P182 = null!;
    public IPublisher<ManyNotifyEvent_183> P183 = null!;
    public IPublisher<ManyNotifyEvent_184> P184 = null!;
    public IPublisher<ManyNotifyEvent_185> P185 = null!;
    public IPublisher<ManyNotifyEvent_186> P186 = null!;
    public IPublisher<ManyNotifyEvent_187> P187 = null!;
    public IPublisher<ManyNotifyEvent_188> P188 = null!;
    public IPublisher<ManyNotifyEvent_189> P189 = null!;
    public IPublisher<ManyNotifyEvent_190> P190 = null!;
    public IPublisher<ManyNotifyEvent_191> P191 = null!;
    public IPublisher<ManyNotifyEvent_192> P192 = null!;
    public IPublisher<ManyNotifyEvent_193> P193 = null!;
    public IPublisher<ManyNotifyEvent_194> P194 = null!;
    public IPublisher<ManyNotifyEvent_195> P195 = null!;
    public IPublisher<ManyNotifyEvent_196> P196 = null!;
    public IPublisher<ManyNotifyEvent_197> P197 = null!;
    public IPublisher<ManyNotifyEvent_198> P198 = null!;
    public IPublisher<ManyNotifyEvent_199> P199 = null!;
    public IPublisher<ManyNotifyEvent_200> P200 = null!;
    public IPublisher<ManyNotifyEvent_201> P201 = null!;
    public IPublisher<ManyNotifyEvent_202> P202 = null!;
    public IPublisher<ManyNotifyEvent_203> P203 = null!;
    public IPublisher<ManyNotifyEvent_204> P204 = null!;
    public IPublisher<ManyNotifyEvent_205> P205 = null!;
    public IPublisher<ManyNotifyEvent_206> P206 = null!;
    public IPublisher<ManyNotifyEvent_207> P207 = null!;
    public IPublisher<ManyNotifyEvent_208> P208 = null!;
    public IPublisher<ManyNotifyEvent_209> P209 = null!;
    public IPublisher<ManyNotifyEvent_210> P210 = null!;
    public IPublisher<ManyNotifyEvent_211> P211 = null!;
    public IPublisher<ManyNotifyEvent_212> P212 = null!;
    public IPublisher<ManyNotifyEvent_213> P213 = null!;
    public IPublisher<ManyNotifyEvent_214> P214 = null!;
    public IPublisher<ManyNotifyEvent_215> P215 = null!;
    public IPublisher<ManyNotifyEvent_216> P216 = null!;
    public IPublisher<ManyNotifyEvent_217> P217 = null!;
    public IPublisher<ManyNotifyEvent_218> P218 = null!;
    public IPublisher<ManyNotifyEvent_219> P219 = null!;
    public IPublisher<ManyNotifyEvent_220> P220 = null!;
    public IPublisher<ManyNotifyEvent_221> P221 = null!;
    public IPublisher<ManyNotifyEvent_222> P222 = null!;
    public IPublisher<ManyNotifyEvent_223> P223 = null!;
    public IPublisher<ManyNotifyEvent_224> P224 = null!;
    public IPublisher<ManyNotifyEvent_225> P225 = null!;
    public IPublisher<ManyNotifyEvent_226> P226 = null!;
    public IPublisher<ManyNotifyEvent_227> P227 = null!;
    public IPublisher<ManyNotifyEvent_228> P228 = null!;
    public IPublisher<ManyNotifyEvent_229> P229 = null!;
    public IPublisher<ManyNotifyEvent_230> P230 = null!;
    public IPublisher<ManyNotifyEvent_231> P231 = null!;
    public IPublisher<ManyNotifyEvent_232> P232 = null!;
    public IPublisher<ManyNotifyEvent_233> P233 = null!;
    public IPublisher<ManyNotifyEvent_234> P234 = null!;
    public IPublisher<ManyNotifyEvent_235> P235 = null!;
    public IPublisher<ManyNotifyEvent_236> P236 = null!;
    public IPublisher<ManyNotifyEvent_237> P237 = null!;
    public IPublisher<ManyNotifyEvent_238> P238 = null!;
    public IPublisher<ManyNotifyEvent_239> P239 = null!;
    public IPublisher<ManyNotifyEvent_240> P240 = null!;
    public IPublisher<ManyNotifyEvent_241> P241 = null!;
    public IPublisher<ManyNotifyEvent_242> P242 = null!;
    public IPublisher<ManyNotifyEvent_243> P243 = null!;
    public IPublisher<ManyNotifyEvent_244> P244 = null!;
    public IPublisher<ManyNotifyEvent_245> P245 = null!;
    public IPublisher<ManyNotifyEvent_246> P246 = null!;
    public IPublisher<ManyNotifyEvent_247> P247 = null!;
    public IPublisher<ManyNotifyEvent_248> P248 = null!;
    public IPublisher<ManyNotifyEvent_249> P249 = null!;
    public IPublisher<ManyNotifyEvent_250> P250 = null!;
    public IPublisher<ManyNotifyEvent_251> P251 = null!;
    public IPublisher<ManyNotifyEvent_252> P252 = null!;
    public IPublisher<ManyNotifyEvent_253> P253 = null!;
    public IPublisher<ManyNotifyEvent_254> P254 = null!;
    public IPublisher<ManyNotifyEvent_255> P255 = null!;
}

internal static class ManyNotifyFixedBatchRegistry
{
    private static void RegisterServiceCopies<TManager>(CompareLayer layer, int subscribersPerEvent)
        where TManager : IService, new()
    {
        for (var i = 0; i < subscribersPerEvent; i++)
        {
            var manager = new TManager();
            if (i == 0)
            {
                // 第一个实例通过正常 RegisterService 注册，Build 时会自动触发其 AutoBind
                layer.RegisterService(manager);
            }
            else
            {
                // 后续实例手动触发 AutoBind
                if (manager is IAutoSubscribe auto)
                    auto.AutoBind(layer);
            }
        }
    }

    private static void SubscribeCopies<TEvent>(IServiceProvider  provider, int subscribersPerEvent,
                                                List<IDisposable> subscriptions)
        where TEvent : struct, IManyNotifyEventPayload
    {
        var subscriber = provider.GetRequiredService<ISubscriber<TEvent>>();
        for (var i = 0; i < subscribersPerEvent; i++)
            subscriptions.Add(subscriber.Subscribe(HandleMessagePipe));
    }

    private static void HandleMessagePipe<TEvent>(TEvent value) where TEvent : struct, IManyNotifyEventPayload
    {
        Volatile.Write(ref CompareSink.IntValue, value.Value);
    }

    private static void DirectConsume<TEvent>(in TEvent value) where TEvent : struct, IManyNotifyEventPayload
    {
        Volatile.Write(ref CompareSink.IntValue, value.Value);
    }

    public static void RegisterLayerBase32(CompareLayer layer, int subscribersPerEvent)
    {
        RegisterServiceCopies<ManyNotifyManager_000>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_001>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_002>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_003>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_004>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_005>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_006>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_007>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_008>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_009>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_010>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_011>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_012>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_013>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_014>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_015>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_016>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_017>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_018>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_019>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_020>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_021>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_022>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_023>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_024>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_025>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_026>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_027>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_028>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_029>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_030>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_031>(layer, subscribersPerEvent);
    }

    public static ManyNotifyBatch32Publishers CreatePublishers32(IServiceProvider  provider, int subscribersPerEvent,
                                                                 List<IDisposable> subscriptions)
    {
        var publishers = new ManyNotifyBatch32Publishers();
        SubscribeCopies<ManyNotifyEvent_000>(provider, subscribersPerEvent, subscriptions);
        publishers.P000 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_000>>();
        SubscribeCopies<ManyNotifyEvent_001>(provider, subscribersPerEvent, subscriptions);
        publishers.P001 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_001>>();
        SubscribeCopies<ManyNotifyEvent_002>(provider, subscribersPerEvent, subscriptions);
        publishers.P002 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_002>>();
        SubscribeCopies<ManyNotifyEvent_003>(provider, subscribersPerEvent, subscriptions);
        publishers.P003 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_003>>();
        SubscribeCopies<ManyNotifyEvent_004>(provider, subscribersPerEvent, subscriptions);
        publishers.P004 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_004>>();
        SubscribeCopies<ManyNotifyEvent_005>(provider, subscribersPerEvent, subscriptions);
        publishers.P005 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_005>>();
        SubscribeCopies<ManyNotifyEvent_006>(provider, subscribersPerEvent, subscriptions);
        publishers.P006 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_006>>();
        SubscribeCopies<ManyNotifyEvent_007>(provider, subscribersPerEvent, subscriptions);
        publishers.P007 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_007>>();
        SubscribeCopies<ManyNotifyEvent_008>(provider, subscribersPerEvent, subscriptions);
        publishers.P008 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_008>>();
        SubscribeCopies<ManyNotifyEvent_009>(provider, subscribersPerEvent, subscriptions);
        publishers.P009 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_009>>();
        SubscribeCopies<ManyNotifyEvent_010>(provider, subscribersPerEvent, subscriptions);
        publishers.P010 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_010>>();
        SubscribeCopies<ManyNotifyEvent_011>(provider, subscribersPerEvent, subscriptions);
        publishers.P011 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_011>>();
        SubscribeCopies<ManyNotifyEvent_012>(provider, subscribersPerEvent, subscriptions);
        publishers.P012 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_012>>();
        SubscribeCopies<ManyNotifyEvent_013>(provider, subscribersPerEvent, subscriptions);
        publishers.P013 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_013>>();
        SubscribeCopies<ManyNotifyEvent_014>(provider, subscribersPerEvent, subscriptions);
        publishers.P014 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_014>>();
        SubscribeCopies<ManyNotifyEvent_015>(provider, subscribersPerEvent, subscriptions);
        publishers.P015 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_015>>();
        SubscribeCopies<ManyNotifyEvent_016>(provider, subscribersPerEvent, subscriptions);
        publishers.P016 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_016>>();
        SubscribeCopies<ManyNotifyEvent_017>(provider, subscribersPerEvent, subscriptions);
        publishers.P017 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_017>>();
        SubscribeCopies<ManyNotifyEvent_018>(provider, subscribersPerEvent, subscriptions);
        publishers.P018 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_018>>();
        SubscribeCopies<ManyNotifyEvent_019>(provider, subscribersPerEvent, subscriptions);
        publishers.P019 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_019>>();
        SubscribeCopies<ManyNotifyEvent_020>(provider, subscribersPerEvent, subscriptions);
        publishers.P020 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_020>>();
        SubscribeCopies<ManyNotifyEvent_021>(provider, subscribersPerEvent, subscriptions);
        publishers.P021 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_021>>();
        SubscribeCopies<ManyNotifyEvent_022>(provider, subscribersPerEvent, subscriptions);
        publishers.P022 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_022>>();
        SubscribeCopies<ManyNotifyEvent_023>(provider, subscribersPerEvent, subscriptions);
        publishers.P023 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_023>>();
        SubscribeCopies<ManyNotifyEvent_024>(provider, subscribersPerEvent, subscriptions);
        publishers.P024 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_024>>();
        SubscribeCopies<ManyNotifyEvent_025>(provider, subscribersPerEvent, subscriptions);
        publishers.P025 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_025>>();
        SubscribeCopies<ManyNotifyEvent_026>(provider, subscribersPerEvent, subscriptions);
        publishers.P026 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_026>>();
        SubscribeCopies<ManyNotifyEvent_027>(provider, subscribersPerEvent, subscriptions);
        publishers.P027 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_027>>();
        SubscribeCopies<ManyNotifyEvent_028>(provider, subscribersPerEvent, subscriptions);
        publishers.P028 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_028>>();
        SubscribeCopies<ManyNotifyEvent_029>(provider, subscribersPerEvent, subscriptions);
        publishers.P029 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_029>>();
        SubscribeCopies<ManyNotifyEvent_030>(provider, subscribersPerEvent, subscriptions);
        publishers.P030 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_030>>();
        SubscribeCopies<ManyNotifyEvent_031>(provider, subscribersPerEvent, subscriptions);
        publishers.P031 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_031>>();
        return publishers;
    }

    public static void DispatchDirect32(int subscribersPerEvent)
    {
        if (subscribersPerEvent == 2)
        {
            DirectConsume(in ManyNotifyEvent_000.Instance);
            DirectConsume(in ManyNotifyEvent_000.Instance);
            DirectConsume(in ManyNotifyEvent_001.Instance);
            DirectConsume(in ManyNotifyEvent_001.Instance);
            DirectConsume(in ManyNotifyEvent_002.Instance);
            DirectConsume(in ManyNotifyEvent_002.Instance);
            DirectConsume(in ManyNotifyEvent_003.Instance);
            DirectConsume(in ManyNotifyEvent_003.Instance);
            DirectConsume(in ManyNotifyEvent_004.Instance);
            DirectConsume(in ManyNotifyEvent_004.Instance);
            DirectConsume(in ManyNotifyEvent_005.Instance);
            DirectConsume(in ManyNotifyEvent_005.Instance);
            DirectConsume(in ManyNotifyEvent_006.Instance);
            DirectConsume(in ManyNotifyEvent_006.Instance);
            DirectConsume(in ManyNotifyEvent_007.Instance);
            DirectConsume(in ManyNotifyEvent_007.Instance);
            DirectConsume(in ManyNotifyEvent_008.Instance);
            DirectConsume(in ManyNotifyEvent_008.Instance);
            DirectConsume(in ManyNotifyEvent_009.Instance);
            DirectConsume(in ManyNotifyEvent_009.Instance);
            DirectConsume(in ManyNotifyEvent_010.Instance);
            DirectConsume(in ManyNotifyEvent_010.Instance);
            DirectConsume(in ManyNotifyEvent_011.Instance);
            DirectConsume(in ManyNotifyEvent_011.Instance);
            DirectConsume(in ManyNotifyEvent_012.Instance);
            DirectConsume(in ManyNotifyEvent_012.Instance);
            DirectConsume(in ManyNotifyEvent_013.Instance);
            DirectConsume(in ManyNotifyEvent_013.Instance);
            DirectConsume(in ManyNotifyEvent_014.Instance);
            DirectConsume(in ManyNotifyEvent_014.Instance);
            DirectConsume(in ManyNotifyEvent_015.Instance);
            DirectConsume(in ManyNotifyEvent_015.Instance);
            DirectConsume(in ManyNotifyEvent_016.Instance);
            DirectConsume(in ManyNotifyEvent_016.Instance);
            DirectConsume(in ManyNotifyEvent_017.Instance);
            DirectConsume(in ManyNotifyEvent_017.Instance);
            DirectConsume(in ManyNotifyEvent_018.Instance);
            DirectConsume(in ManyNotifyEvent_018.Instance);
            DirectConsume(in ManyNotifyEvent_019.Instance);
            DirectConsume(in ManyNotifyEvent_019.Instance);
            DirectConsume(in ManyNotifyEvent_020.Instance);
            DirectConsume(in ManyNotifyEvent_020.Instance);
            DirectConsume(in ManyNotifyEvent_021.Instance);
            DirectConsume(in ManyNotifyEvent_021.Instance);
            DirectConsume(in ManyNotifyEvent_022.Instance);
            DirectConsume(in ManyNotifyEvent_022.Instance);
            DirectConsume(in ManyNotifyEvent_023.Instance);
            DirectConsume(in ManyNotifyEvent_023.Instance);
            DirectConsume(in ManyNotifyEvent_024.Instance);
            DirectConsume(in ManyNotifyEvent_024.Instance);
            DirectConsume(in ManyNotifyEvent_025.Instance);
            DirectConsume(in ManyNotifyEvent_025.Instance);
            DirectConsume(in ManyNotifyEvent_026.Instance);
            DirectConsume(in ManyNotifyEvent_026.Instance);
            DirectConsume(in ManyNotifyEvent_027.Instance);
            DirectConsume(in ManyNotifyEvent_027.Instance);
            DirectConsume(in ManyNotifyEvent_028.Instance);
            DirectConsume(in ManyNotifyEvent_028.Instance);
            DirectConsume(in ManyNotifyEvent_029.Instance);
            DirectConsume(in ManyNotifyEvent_029.Instance);
            DirectConsume(in ManyNotifyEvent_030.Instance);
            DirectConsume(in ManyNotifyEvent_030.Instance);
            DirectConsume(in ManyNotifyEvent_031.Instance);
            DirectConsume(in ManyNotifyEvent_031.Instance);
            return;
        }

        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
    }

    public static void DispatchLayerBase32()
    {
        LayerHub.Send(ManyNotifyEvent_000.Instance);
        LayerHub.Send(ManyNotifyEvent_001.Instance);
        LayerHub.Send(ManyNotifyEvent_002.Instance);
        LayerHub.Send(ManyNotifyEvent_003.Instance);
        LayerHub.Send(ManyNotifyEvent_004.Instance);
        LayerHub.Send(ManyNotifyEvent_005.Instance);
        LayerHub.Send(ManyNotifyEvent_006.Instance);
        LayerHub.Send(ManyNotifyEvent_007.Instance);
        LayerHub.Send(ManyNotifyEvent_008.Instance);
        LayerHub.Send(ManyNotifyEvent_009.Instance);
        LayerHub.Send(ManyNotifyEvent_010.Instance);
        LayerHub.Send(ManyNotifyEvent_011.Instance);
        LayerHub.Send(ManyNotifyEvent_012.Instance);
        LayerHub.Send(ManyNotifyEvent_013.Instance);
        LayerHub.Send(ManyNotifyEvent_014.Instance);
        LayerHub.Send(ManyNotifyEvent_015.Instance);
        LayerHub.Send(ManyNotifyEvent_016.Instance);
        LayerHub.Send(ManyNotifyEvent_017.Instance);
        LayerHub.Send(ManyNotifyEvent_018.Instance);
        LayerHub.Send(ManyNotifyEvent_019.Instance);
        LayerHub.Send(ManyNotifyEvent_020.Instance);
        LayerHub.Send(ManyNotifyEvent_021.Instance);
        LayerHub.Send(ManyNotifyEvent_022.Instance);
        LayerHub.Send(ManyNotifyEvent_023.Instance);
        LayerHub.Send(ManyNotifyEvent_024.Instance);
        LayerHub.Send(ManyNotifyEvent_025.Instance);
        LayerHub.Send(ManyNotifyEvent_026.Instance);
        LayerHub.Send(ManyNotifyEvent_027.Instance);
        LayerHub.Send(ManyNotifyEvent_028.Instance);
        LayerHub.Send(ManyNotifyEvent_029.Instance);
        LayerHub.Send(ManyNotifyEvent_030.Instance);
        LayerHub.Send(ManyNotifyEvent_031.Instance);
    }

    public static void DispatchMessagePipe32(ManyNotifyBatch32Publishers publishers)
    {
        publishers.P000.Publish(ManyNotifyEvent_000.Instance);
        publishers.P001.Publish(ManyNotifyEvent_001.Instance);
        publishers.P002.Publish(ManyNotifyEvent_002.Instance);
        publishers.P003.Publish(ManyNotifyEvent_003.Instance);
        publishers.P004.Publish(ManyNotifyEvent_004.Instance);
        publishers.P005.Publish(ManyNotifyEvent_005.Instance);
        publishers.P006.Publish(ManyNotifyEvent_006.Instance);
        publishers.P007.Publish(ManyNotifyEvent_007.Instance);
        publishers.P008.Publish(ManyNotifyEvent_008.Instance);
        publishers.P009.Publish(ManyNotifyEvent_009.Instance);
        publishers.P010.Publish(ManyNotifyEvent_010.Instance);
        publishers.P011.Publish(ManyNotifyEvent_011.Instance);
        publishers.P012.Publish(ManyNotifyEvent_012.Instance);
        publishers.P013.Publish(ManyNotifyEvent_013.Instance);
        publishers.P014.Publish(ManyNotifyEvent_014.Instance);
        publishers.P015.Publish(ManyNotifyEvent_015.Instance);
        publishers.P016.Publish(ManyNotifyEvent_016.Instance);
        publishers.P017.Publish(ManyNotifyEvent_017.Instance);
        publishers.P018.Publish(ManyNotifyEvent_018.Instance);
        publishers.P019.Publish(ManyNotifyEvent_019.Instance);
        publishers.P020.Publish(ManyNotifyEvent_020.Instance);
        publishers.P021.Publish(ManyNotifyEvent_021.Instance);
        publishers.P022.Publish(ManyNotifyEvent_022.Instance);
        publishers.P023.Publish(ManyNotifyEvent_023.Instance);
        publishers.P024.Publish(ManyNotifyEvent_024.Instance);
        publishers.P025.Publish(ManyNotifyEvent_025.Instance);
        publishers.P026.Publish(ManyNotifyEvent_026.Instance);
        publishers.P027.Publish(ManyNotifyEvent_027.Instance);
        publishers.P028.Publish(ManyNotifyEvent_028.Instance);
        publishers.P029.Publish(ManyNotifyEvent_029.Instance);
        publishers.P030.Publish(ManyNotifyEvent_030.Instance);
        publishers.P031.Publish(ManyNotifyEvent_031.Instance);
    }

    public static void RegisterLayerBase128(CompareLayer layer, int subscribersPerEvent)
    {
        RegisterServiceCopies<ManyNotifyManager_000>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_001>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_002>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_003>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_004>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_005>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_006>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_007>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_008>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_009>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_010>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_011>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_012>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_013>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_014>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_015>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_016>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_017>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_018>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_019>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_020>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_021>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_022>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_023>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_024>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_025>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_026>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_027>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_028>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_029>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_030>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_031>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_032>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_033>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_034>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_035>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_036>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_037>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_038>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_039>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_040>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_041>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_042>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_043>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_044>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_045>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_046>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_047>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_048>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_049>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_050>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_051>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_052>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_053>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_054>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_055>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_056>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_057>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_058>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_059>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_060>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_061>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_062>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_063>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_064>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_065>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_066>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_067>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_068>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_069>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_070>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_071>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_072>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_073>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_074>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_075>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_076>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_077>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_078>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_079>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_080>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_081>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_082>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_083>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_084>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_085>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_086>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_087>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_088>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_089>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_090>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_091>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_092>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_093>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_094>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_095>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_096>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_097>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_098>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_099>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_100>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_101>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_102>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_103>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_104>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_105>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_106>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_107>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_108>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_109>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_110>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_111>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_112>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_113>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_114>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_115>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_116>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_117>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_118>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_119>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_120>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_121>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_122>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_123>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_124>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_125>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_126>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_127>(layer, subscribersPerEvent);
    }

    public static ManyNotifyBatch128Publishers CreatePublishers128(IServiceProvider  provider, int subscribersPerEvent,
                                                                   List<IDisposable> subscriptions)
    {
        var publishers = new ManyNotifyBatch128Publishers();
        SubscribeCopies<ManyNotifyEvent_000>(provider, subscribersPerEvent, subscriptions);
        publishers.P000 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_000>>();
        SubscribeCopies<ManyNotifyEvent_001>(provider, subscribersPerEvent, subscriptions);
        publishers.P001 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_001>>();
        SubscribeCopies<ManyNotifyEvent_002>(provider, subscribersPerEvent, subscriptions);
        publishers.P002 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_002>>();
        SubscribeCopies<ManyNotifyEvent_003>(provider, subscribersPerEvent, subscriptions);
        publishers.P003 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_003>>();
        SubscribeCopies<ManyNotifyEvent_004>(provider, subscribersPerEvent, subscriptions);
        publishers.P004 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_004>>();
        SubscribeCopies<ManyNotifyEvent_005>(provider, subscribersPerEvent, subscriptions);
        publishers.P005 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_005>>();
        SubscribeCopies<ManyNotifyEvent_006>(provider, subscribersPerEvent, subscriptions);
        publishers.P006 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_006>>();
        SubscribeCopies<ManyNotifyEvent_007>(provider, subscribersPerEvent, subscriptions);
        publishers.P007 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_007>>();
        SubscribeCopies<ManyNotifyEvent_008>(provider, subscribersPerEvent, subscriptions);
        publishers.P008 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_008>>();
        SubscribeCopies<ManyNotifyEvent_009>(provider, subscribersPerEvent, subscriptions);
        publishers.P009 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_009>>();
        SubscribeCopies<ManyNotifyEvent_010>(provider, subscribersPerEvent, subscriptions);
        publishers.P010 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_010>>();
        SubscribeCopies<ManyNotifyEvent_011>(provider, subscribersPerEvent, subscriptions);
        publishers.P011 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_011>>();
        SubscribeCopies<ManyNotifyEvent_012>(provider, subscribersPerEvent, subscriptions);
        publishers.P012 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_012>>();
        SubscribeCopies<ManyNotifyEvent_013>(provider, subscribersPerEvent, subscriptions);
        publishers.P013 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_013>>();
        SubscribeCopies<ManyNotifyEvent_014>(provider, subscribersPerEvent, subscriptions);
        publishers.P014 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_014>>();
        SubscribeCopies<ManyNotifyEvent_015>(provider, subscribersPerEvent, subscriptions);
        publishers.P015 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_015>>();
        SubscribeCopies<ManyNotifyEvent_016>(provider, subscribersPerEvent, subscriptions);
        publishers.P016 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_016>>();
        SubscribeCopies<ManyNotifyEvent_017>(provider, subscribersPerEvent, subscriptions);
        publishers.P017 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_017>>();
        SubscribeCopies<ManyNotifyEvent_018>(provider, subscribersPerEvent, subscriptions);
        publishers.P018 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_018>>();
        SubscribeCopies<ManyNotifyEvent_019>(provider, subscribersPerEvent, subscriptions);
        publishers.P019 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_019>>();
        SubscribeCopies<ManyNotifyEvent_020>(provider, subscribersPerEvent, subscriptions);
        publishers.P020 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_020>>();
        SubscribeCopies<ManyNotifyEvent_021>(provider, subscribersPerEvent, subscriptions);
        publishers.P021 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_021>>();
        SubscribeCopies<ManyNotifyEvent_022>(provider, subscribersPerEvent, subscriptions);
        publishers.P022 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_022>>();
        SubscribeCopies<ManyNotifyEvent_023>(provider, subscribersPerEvent, subscriptions);
        publishers.P023 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_023>>();
        SubscribeCopies<ManyNotifyEvent_024>(provider, subscribersPerEvent, subscriptions);
        publishers.P024 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_024>>();
        SubscribeCopies<ManyNotifyEvent_025>(provider, subscribersPerEvent, subscriptions);
        publishers.P025 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_025>>();
        SubscribeCopies<ManyNotifyEvent_026>(provider, subscribersPerEvent, subscriptions);
        publishers.P026 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_026>>();
        SubscribeCopies<ManyNotifyEvent_027>(provider, subscribersPerEvent, subscriptions);
        publishers.P027 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_027>>();
        SubscribeCopies<ManyNotifyEvent_028>(provider, subscribersPerEvent, subscriptions);
        publishers.P028 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_028>>();
        SubscribeCopies<ManyNotifyEvent_029>(provider, subscribersPerEvent, subscriptions);
        publishers.P029 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_029>>();
        SubscribeCopies<ManyNotifyEvent_030>(provider, subscribersPerEvent, subscriptions);
        publishers.P030 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_030>>();
        SubscribeCopies<ManyNotifyEvent_031>(provider, subscribersPerEvent, subscriptions);
        publishers.P031 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_031>>();
        SubscribeCopies<ManyNotifyEvent_032>(provider, subscribersPerEvent, subscriptions);
        publishers.P032 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_032>>();
        SubscribeCopies<ManyNotifyEvent_033>(provider, subscribersPerEvent, subscriptions);
        publishers.P033 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_033>>();
        SubscribeCopies<ManyNotifyEvent_034>(provider, subscribersPerEvent, subscriptions);
        publishers.P034 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_034>>();
        SubscribeCopies<ManyNotifyEvent_035>(provider, subscribersPerEvent, subscriptions);
        publishers.P035 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_035>>();
        SubscribeCopies<ManyNotifyEvent_036>(provider, subscribersPerEvent, subscriptions);
        publishers.P036 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_036>>();
        SubscribeCopies<ManyNotifyEvent_037>(provider, subscribersPerEvent, subscriptions);
        publishers.P037 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_037>>();
        SubscribeCopies<ManyNotifyEvent_038>(provider, subscribersPerEvent, subscriptions);
        publishers.P038 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_038>>();
        SubscribeCopies<ManyNotifyEvent_039>(provider, subscribersPerEvent, subscriptions);
        publishers.P039 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_039>>();
        SubscribeCopies<ManyNotifyEvent_040>(provider, subscribersPerEvent, subscriptions);
        publishers.P040 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_040>>();
        SubscribeCopies<ManyNotifyEvent_041>(provider, subscribersPerEvent, subscriptions);
        publishers.P041 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_041>>();
        SubscribeCopies<ManyNotifyEvent_042>(provider, subscribersPerEvent, subscriptions);
        publishers.P042 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_042>>();
        SubscribeCopies<ManyNotifyEvent_043>(provider, subscribersPerEvent, subscriptions);
        publishers.P043 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_043>>();
        SubscribeCopies<ManyNotifyEvent_044>(provider, subscribersPerEvent, subscriptions);
        publishers.P044 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_044>>();
        SubscribeCopies<ManyNotifyEvent_045>(provider, subscribersPerEvent, subscriptions);
        publishers.P045 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_045>>();
        SubscribeCopies<ManyNotifyEvent_046>(provider, subscribersPerEvent, subscriptions);
        publishers.P046 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_046>>();
        SubscribeCopies<ManyNotifyEvent_047>(provider, subscribersPerEvent, subscriptions);
        publishers.P047 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_047>>();
        SubscribeCopies<ManyNotifyEvent_048>(provider, subscribersPerEvent, subscriptions);
        publishers.P048 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_048>>();
        SubscribeCopies<ManyNotifyEvent_049>(provider, subscribersPerEvent, subscriptions);
        publishers.P049 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_049>>();
        SubscribeCopies<ManyNotifyEvent_050>(provider, subscribersPerEvent, subscriptions);
        publishers.P050 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_050>>();
        SubscribeCopies<ManyNotifyEvent_051>(provider, subscribersPerEvent, subscriptions);
        publishers.P051 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_051>>();
        SubscribeCopies<ManyNotifyEvent_052>(provider, subscribersPerEvent, subscriptions);
        publishers.P052 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_052>>();
        SubscribeCopies<ManyNotifyEvent_053>(provider, subscribersPerEvent, subscriptions);
        publishers.P053 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_053>>();
        SubscribeCopies<ManyNotifyEvent_054>(provider, subscribersPerEvent, subscriptions);
        publishers.P054 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_054>>();
        SubscribeCopies<ManyNotifyEvent_055>(provider, subscribersPerEvent, subscriptions);
        publishers.P055 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_055>>();
        SubscribeCopies<ManyNotifyEvent_056>(provider, subscribersPerEvent, subscriptions);
        publishers.P056 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_056>>();
        SubscribeCopies<ManyNotifyEvent_057>(provider, subscribersPerEvent, subscriptions);
        publishers.P057 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_057>>();
        SubscribeCopies<ManyNotifyEvent_058>(provider, subscribersPerEvent, subscriptions);
        publishers.P058 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_058>>();
        SubscribeCopies<ManyNotifyEvent_059>(provider, subscribersPerEvent, subscriptions);
        publishers.P059 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_059>>();
        SubscribeCopies<ManyNotifyEvent_060>(provider, subscribersPerEvent, subscriptions);
        publishers.P060 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_060>>();
        SubscribeCopies<ManyNotifyEvent_061>(provider, subscribersPerEvent, subscriptions);
        publishers.P061 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_061>>();
        SubscribeCopies<ManyNotifyEvent_062>(provider, subscribersPerEvent, subscriptions);
        publishers.P062 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_062>>();
        SubscribeCopies<ManyNotifyEvent_063>(provider, subscribersPerEvent, subscriptions);
        publishers.P063 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_063>>();
        SubscribeCopies<ManyNotifyEvent_064>(provider, subscribersPerEvent, subscriptions);
        publishers.P064 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_064>>();
        SubscribeCopies<ManyNotifyEvent_065>(provider, subscribersPerEvent, subscriptions);
        publishers.P065 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_065>>();
        SubscribeCopies<ManyNotifyEvent_066>(provider, subscribersPerEvent, subscriptions);
        publishers.P066 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_066>>();
        SubscribeCopies<ManyNotifyEvent_067>(provider, subscribersPerEvent, subscriptions);
        publishers.P067 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_067>>();
        SubscribeCopies<ManyNotifyEvent_068>(provider, subscribersPerEvent, subscriptions);
        publishers.P068 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_068>>();
        SubscribeCopies<ManyNotifyEvent_069>(provider, subscribersPerEvent, subscriptions);
        publishers.P069 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_069>>();
        SubscribeCopies<ManyNotifyEvent_070>(provider, subscribersPerEvent, subscriptions);
        publishers.P070 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_070>>();
        SubscribeCopies<ManyNotifyEvent_071>(provider, subscribersPerEvent, subscriptions);
        publishers.P071 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_071>>();
        SubscribeCopies<ManyNotifyEvent_072>(provider, subscribersPerEvent, subscriptions);
        publishers.P072 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_072>>();
        SubscribeCopies<ManyNotifyEvent_073>(provider, subscribersPerEvent, subscriptions);
        publishers.P073 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_073>>();
        SubscribeCopies<ManyNotifyEvent_074>(provider, subscribersPerEvent, subscriptions);
        publishers.P074 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_074>>();
        SubscribeCopies<ManyNotifyEvent_075>(provider, subscribersPerEvent, subscriptions);
        publishers.P075 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_075>>();
        SubscribeCopies<ManyNotifyEvent_076>(provider, subscribersPerEvent, subscriptions);
        publishers.P076 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_076>>();
        SubscribeCopies<ManyNotifyEvent_077>(provider, subscribersPerEvent, subscriptions);
        publishers.P077 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_077>>();
        SubscribeCopies<ManyNotifyEvent_078>(provider, subscribersPerEvent, subscriptions);
        publishers.P078 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_078>>();
        SubscribeCopies<ManyNotifyEvent_079>(provider, subscribersPerEvent, subscriptions);
        publishers.P079 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_079>>();
        SubscribeCopies<ManyNotifyEvent_080>(provider, subscribersPerEvent, subscriptions);
        publishers.P080 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_080>>();
        SubscribeCopies<ManyNotifyEvent_081>(provider, subscribersPerEvent, subscriptions);
        publishers.P081 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_081>>();
        SubscribeCopies<ManyNotifyEvent_082>(provider, subscribersPerEvent, subscriptions);
        publishers.P082 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_082>>();
        SubscribeCopies<ManyNotifyEvent_083>(provider, subscribersPerEvent, subscriptions);
        publishers.P083 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_083>>();
        SubscribeCopies<ManyNotifyEvent_084>(provider, subscribersPerEvent, subscriptions);
        publishers.P084 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_084>>();
        SubscribeCopies<ManyNotifyEvent_085>(provider, subscribersPerEvent, subscriptions);
        publishers.P085 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_085>>();
        SubscribeCopies<ManyNotifyEvent_086>(provider, subscribersPerEvent, subscriptions);
        publishers.P086 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_086>>();
        SubscribeCopies<ManyNotifyEvent_087>(provider, subscribersPerEvent, subscriptions);
        publishers.P087 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_087>>();
        SubscribeCopies<ManyNotifyEvent_088>(provider, subscribersPerEvent, subscriptions);
        publishers.P088 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_088>>();
        SubscribeCopies<ManyNotifyEvent_089>(provider, subscribersPerEvent, subscriptions);
        publishers.P089 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_089>>();
        SubscribeCopies<ManyNotifyEvent_090>(provider, subscribersPerEvent, subscriptions);
        publishers.P090 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_090>>();
        SubscribeCopies<ManyNotifyEvent_091>(provider, subscribersPerEvent, subscriptions);
        publishers.P091 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_091>>();
        SubscribeCopies<ManyNotifyEvent_092>(provider, subscribersPerEvent, subscriptions);
        publishers.P092 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_092>>();
        SubscribeCopies<ManyNotifyEvent_093>(provider, subscribersPerEvent, subscriptions);
        publishers.P093 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_093>>();
        SubscribeCopies<ManyNotifyEvent_094>(provider, subscribersPerEvent, subscriptions);
        publishers.P094 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_094>>();
        SubscribeCopies<ManyNotifyEvent_095>(provider, subscribersPerEvent, subscriptions);
        publishers.P095 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_095>>();
        SubscribeCopies<ManyNotifyEvent_096>(provider, subscribersPerEvent, subscriptions);
        publishers.P096 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_096>>();
        SubscribeCopies<ManyNotifyEvent_097>(provider, subscribersPerEvent, subscriptions);
        publishers.P097 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_097>>();
        SubscribeCopies<ManyNotifyEvent_098>(provider, subscribersPerEvent, subscriptions);
        publishers.P098 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_098>>();
        SubscribeCopies<ManyNotifyEvent_099>(provider, subscribersPerEvent, subscriptions);
        publishers.P099 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_099>>();
        SubscribeCopies<ManyNotifyEvent_100>(provider, subscribersPerEvent, subscriptions);
        publishers.P100 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_100>>();
        SubscribeCopies<ManyNotifyEvent_101>(provider, subscribersPerEvent, subscriptions);
        publishers.P101 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_101>>();
        SubscribeCopies<ManyNotifyEvent_102>(provider, subscribersPerEvent, subscriptions);
        publishers.P102 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_102>>();
        SubscribeCopies<ManyNotifyEvent_103>(provider, subscribersPerEvent, subscriptions);
        publishers.P103 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_103>>();
        SubscribeCopies<ManyNotifyEvent_104>(provider, subscribersPerEvent, subscriptions);
        publishers.P104 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_104>>();
        SubscribeCopies<ManyNotifyEvent_105>(provider, subscribersPerEvent, subscriptions);
        publishers.P105 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_105>>();
        SubscribeCopies<ManyNotifyEvent_106>(provider, subscribersPerEvent, subscriptions);
        publishers.P106 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_106>>();
        SubscribeCopies<ManyNotifyEvent_107>(provider, subscribersPerEvent, subscriptions);
        publishers.P107 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_107>>();
        SubscribeCopies<ManyNotifyEvent_108>(provider, subscribersPerEvent, subscriptions);
        publishers.P108 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_108>>();
        SubscribeCopies<ManyNotifyEvent_109>(provider, subscribersPerEvent, subscriptions);
        publishers.P109 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_109>>();
        SubscribeCopies<ManyNotifyEvent_110>(provider, subscribersPerEvent, subscriptions);
        publishers.P110 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_110>>();
        SubscribeCopies<ManyNotifyEvent_111>(provider, subscribersPerEvent, subscriptions);
        publishers.P111 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_111>>();
        SubscribeCopies<ManyNotifyEvent_112>(provider, subscribersPerEvent, subscriptions);
        publishers.P112 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_112>>();
        SubscribeCopies<ManyNotifyEvent_113>(provider, subscribersPerEvent, subscriptions);
        publishers.P113 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_113>>();
        SubscribeCopies<ManyNotifyEvent_114>(provider, subscribersPerEvent, subscriptions);
        publishers.P114 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_114>>();
        SubscribeCopies<ManyNotifyEvent_115>(provider, subscribersPerEvent, subscriptions);
        publishers.P115 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_115>>();
        SubscribeCopies<ManyNotifyEvent_116>(provider, subscribersPerEvent, subscriptions);
        publishers.P116 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_116>>();
        SubscribeCopies<ManyNotifyEvent_117>(provider, subscribersPerEvent, subscriptions);
        publishers.P117 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_117>>();
        SubscribeCopies<ManyNotifyEvent_118>(provider, subscribersPerEvent, subscriptions);
        publishers.P118 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_118>>();
        SubscribeCopies<ManyNotifyEvent_119>(provider, subscribersPerEvent, subscriptions);
        publishers.P119 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_119>>();
        SubscribeCopies<ManyNotifyEvent_120>(provider, subscribersPerEvent, subscriptions);
        publishers.P120 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_120>>();
        SubscribeCopies<ManyNotifyEvent_121>(provider, subscribersPerEvent, subscriptions);
        publishers.P121 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_121>>();
        SubscribeCopies<ManyNotifyEvent_122>(provider, subscribersPerEvent, subscriptions);
        publishers.P122 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_122>>();
        SubscribeCopies<ManyNotifyEvent_123>(provider, subscribersPerEvent, subscriptions);
        publishers.P123 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_123>>();
        SubscribeCopies<ManyNotifyEvent_124>(provider, subscribersPerEvent, subscriptions);
        publishers.P124 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_124>>();
        SubscribeCopies<ManyNotifyEvent_125>(provider, subscribersPerEvent, subscriptions);
        publishers.P125 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_125>>();
        SubscribeCopies<ManyNotifyEvent_126>(provider, subscribersPerEvent, subscriptions);
        publishers.P126 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_126>>();
        SubscribeCopies<ManyNotifyEvent_127>(provider, subscribersPerEvent, subscriptions);
        publishers.P127 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_127>>();
        return publishers;
    }

    public static void DispatchDirect128(int subscribersPerEvent)
    {
        if (subscribersPerEvent == 2)
        {
            DirectConsume(in ManyNotifyEvent_000.Instance);
            DirectConsume(in ManyNotifyEvent_000.Instance);
            DirectConsume(in ManyNotifyEvent_001.Instance);
            DirectConsume(in ManyNotifyEvent_001.Instance);
            DirectConsume(in ManyNotifyEvent_002.Instance);
            DirectConsume(in ManyNotifyEvent_002.Instance);
            DirectConsume(in ManyNotifyEvent_003.Instance);
            DirectConsume(in ManyNotifyEvent_003.Instance);
            DirectConsume(in ManyNotifyEvent_004.Instance);
            DirectConsume(in ManyNotifyEvent_004.Instance);
            DirectConsume(in ManyNotifyEvent_005.Instance);
            DirectConsume(in ManyNotifyEvent_005.Instance);
            DirectConsume(in ManyNotifyEvent_006.Instance);
            DirectConsume(in ManyNotifyEvent_006.Instance);
            DirectConsume(in ManyNotifyEvent_007.Instance);
            DirectConsume(in ManyNotifyEvent_007.Instance);
            DirectConsume(in ManyNotifyEvent_008.Instance);
            DirectConsume(in ManyNotifyEvent_008.Instance);
            DirectConsume(in ManyNotifyEvent_009.Instance);
            DirectConsume(in ManyNotifyEvent_009.Instance);
            DirectConsume(in ManyNotifyEvent_010.Instance);
            DirectConsume(in ManyNotifyEvent_010.Instance);
            DirectConsume(in ManyNotifyEvent_011.Instance);
            DirectConsume(in ManyNotifyEvent_011.Instance);
            DirectConsume(in ManyNotifyEvent_012.Instance);
            DirectConsume(in ManyNotifyEvent_012.Instance);
            DirectConsume(in ManyNotifyEvent_013.Instance);
            DirectConsume(in ManyNotifyEvent_013.Instance);
            DirectConsume(in ManyNotifyEvent_014.Instance);
            DirectConsume(in ManyNotifyEvent_014.Instance);
            DirectConsume(in ManyNotifyEvent_015.Instance);
            DirectConsume(in ManyNotifyEvent_015.Instance);
            DirectConsume(in ManyNotifyEvent_016.Instance);
            DirectConsume(in ManyNotifyEvent_016.Instance);
            DirectConsume(in ManyNotifyEvent_017.Instance);
            DirectConsume(in ManyNotifyEvent_017.Instance);
            DirectConsume(in ManyNotifyEvent_018.Instance);
            DirectConsume(in ManyNotifyEvent_018.Instance);
            DirectConsume(in ManyNotifyEvent_019.Instance);
            DirectConsume(in ManyNotifyEvent_019.Instance);
            DirectConsume(in ManyNotifyEvent_020.Instance);
            DirectConsume(in ManyNotifyEvent_020.Instance);
            DirectConsume(in ManyNotifyEvent_021.Instance);
            DirectConsume(in ManyNotifyEvent_021.Instance);
            DirectConsume(in ManyNotifyEvent_022.Instance);
            DirectConsume(in ManyNotifyEvent_022.Instance);
            DirectConsume(in ManyNotifyEvent_023.Instance);
            DirectConsume(in ManyNotifyEvent_023.Instance);
            DirectConsume(in ManyNotifyEvent_024.Instance);
            DirectConsume(in ManyNotifyEvent_024.Instance);
            DirectConsume(in ManyNotifyEvent_025.Instance);
            DirectConsume(in ManyNotifyEvent_025.Instance);
            DirectConsume(in ManyNotifyEvent_026.Instance);
            DirectConsume(in ManyNotifyEvent_026.Instance);
            DirectConsume(in ManyNotifyEvent_027.Instance);
            DirectConsume(in ManyNotifyEvent_027.Instance);
            DirectConsume(in ManyNotifyEvent_028.Instance);
            DirectConsume(in ManyNotifyEvent_028.Instance);
            DirectConsume(in ManyNotifyEvent_029.Instance);
            DirectConsume(in ManyNotifyEvent_029.Instance);
            DirectConsume(in ManyNotifyEvent_030.Instance);
            DirectConsume(in ManyNotifyEvent_030.Instance);
            DirectConsume(in ManyNotifyEvent_031.Instance);
            DirectConsume(in ManyNotifyEvent_031.Instance);
            DirectConsume(in ManyNotifyEvent_032.Instance);
            DirectConsume(in ManyNotifyEvent_032.Instance);
            DirectConsume(in ManyNotifyEvent_033.Instance);
            DirectConsume(in ManyNotifyEvent_033.Instance);
            DirectConsume(in ManyNotifyEvent_034.Instance);
            DirectConsume(in ManyNotifyEvent_034.Instance);
            DirectConsume(in ManyNotifyEvent_035.Instance);
            DirectConsume(in ManyNotifyEvent_035.Instance);
            DirectConsume(in ManyNotifyEvent_036.Instance);
            DirectConsume(in ManyNotifyEvent_036.Instance);
            DirectConsume(in ManyNotifyEvent_037.Instance);
            DirectConsume(in ManyNotifyEvent_037.Instance);
            DirectConsume(in ManyNotifyEvent_038.Instance);
            DirectConsume(in ManyNotifyEvent_038.Instance);
            DirectConsume(in ManyNotifyEvent_039.Instance);
            DirectConsume(in ManyNotifyEvent_039.Instance);
            DirectConsume(in ManyNotifyEvent_040.Instance);
            DirectConsume(in ManyNotifyEvent_040.Instance);
            DirectConsume(in ManyNotifyEvent_041.Instance);
            DirectConsume(in ManyNotifyEvent_041.Instance);
            DirectConsume(in ManyNotifyEvent_042.Instance);
            DirectConsume(in ManyNotifyEvent_042.Instance);
            DirectConsume(in ManyNotifyEvent_043.Instance);
            DirectConsume(in ManyNotifyEvent_043.Instance);
            DirectConsume(in ManyNotifyEvent_044.Instance);
            DirectConsume(in ManyNotifyEvent_044.Instance);
            DirectConsume(in ManyNotifyEvent_045.Instance);
            DirectConsume(in ManyNotifyEvent_045.Instance);
            DirectConsume(in ManyNotifyEvent_046.Instance);
            DirectConsume(in ManyNotifyEvent_046.Instance);
            DirectConsume(in ManyNotifyEvent_047.Instance);
            DirectConsume(in ManyNotifyEvent_047.Instance);
            DirectConsume(in ManyNotifyEvent_048.Instance);
            DirectConsume(in ManyNotifyEvent_048.Instance);
            DirectConsume(in ManyNotifyEvent_049.Instance);
            DirectConsume(in ManyNotifyEvent_049.Instance);
            DirectConsume(in ManyNotifyEvent_050.Instance);
            DirectConsume(in ManyNotifyEvent_050.Instance);
            DirectConsume(in ManyNotifyEvent_051.Instance);
            DirectConsume(in ManyNotifyEvent_051.Instance);
            DirectConsume(in ManyNotifyEvent_052.Instance);
            DirectConsume(in ManyNotifyEvent_052.Instance);
            DirectConsume(in ManyNotifyEvent_053.Instance);
            DirectConsume(in ManyNotifyEvent_053.Instance);
            DirectConsume(in ManyNotifyEvent_054.Instance);
            DirectConsume(in ManyNotifyEvent_054.Instance);
            DirectConsume(in ManyNotifyEvent_055.Instance);
            DirectConsume(in ManyNotifyEvent_055.Instance);
            DirectConsume(in ManyNotifyEvent_056.Instance);
            DirectConsume(in ManyNotifyEvent_056.Instance);
            DirectConsume(in ManyNotifyEvent_057.Instance);
            DirectConsume(in ManyNotifyEvent_057.Instance);
            DirectConsume(in ManyNotifyEvent_058.Instance);
            DirectConsume(in ManyNotifyEvent_058.Instance);
            DirectConsume(in ManyNotifyEvent_059.Instance);
            DirectConsume(in ManyNotifyEvent_059.Instance);
            DirectConsume(in ManyNotifyEvent_060.Instance);
            DirectConsume(in ManyNotifyEvent_060.Instance);
            DirectConsume(in ManyNotifyEvent_061.Instance);
            DirectConsume(in ManyNotifyEvent_061.Instance);
            DirectConsume(in ManyNotifyEvent_062.Instance);
            DirectConsume(in ManyNotifyEvent_062.Instance);
            DirectConsume(in ManyNotifyEvent_063.Instance);
            DirectConsume(in ManyNotifyEvent_063.Instance);
            DirectConsume(in ManyNotifyEvent_064.Instance);
            DirectConsume(in ManyNotifyEvent_064.Instance);
            DirectConsume(in ManyNotifyEvent_065.Instance);
            DirectConsume(in ManyNotifyEvent_065.Instance);
            DirectConsume(in ManyNotifyEvent_066.Instance);
            DirectConsume(in ManyNotifyEvent_066.Instance);
            DirectConsume(in ManyNotifyEvent_067.Instance);
            DirectConsume(in ManyNotifyEvent_067.Instance);
            DirectConsume(in ManyNotifyEvent_068.Instance);
            DirectConsume(in ManyNotifyEvent_068.Instance);
            DirectConsume(in ManyNotifyEvent_069.Instance);
            DirectConsume(in ManyNotifyEvent_069.Instance);
            DirectConsume(in ManyNotifyEvent_070.Instance);
            DirectConsume(in ManyNotifyEvent_070.Instance);
            DirectConsume(in ManyNotifyEvent_071.Instance);
            DirectConsume(in ManyNotifyEvent_071.Instance);
            DirectConsume(in ManyNotifyEvent_072.Instance);
            DirectConsume(in ManyNotifyEvent_072.Instance);
            DirectConsume(in ManyNotifyEvent_073.Instance);
            DirectConsume(in ManyNotifyEvent_073.Instance);
            DirectConsume(in ManyNotifyEvent_074.Instance);
            DirectConsume(in ManyNotifyEvent_074.Instance);
            DirectConsume(in ManyNotifyEvent_075.Instance);
            DirectConsume(in ManyNotifyEvent_075.Instance);
            DirectConsume(in ManyNotifyEvent_076.Instance);
            DirectConsume(in ManyNotifyEvent_076.Instance);
            DirectConsume(in ManyNotifyEvent_077.Instance);
            DirectConsume(in ManyNotifyEvent_077.Instance);
            DirectConsume(in ManyNotifyEvent_078.Instance);
            DirectConsume(in ManyNotifyEvent_078.Instance);
            DirectConsume(in ManyNotifyEvent_079.Instance);
            DirectConsume(in ManyNotifyEvent_079.Instance);
            DirectConsume(in ManyNotifyEvent_080.Instance);
            DirectConsume(in ManyNotifyEvent_080.Instance);
            DirectConsume(in ManyNotifyEvent_081.Instance);
            DirectConsume(in ManyNotifyEvent_081.Instance);
            DirectConsume(in ManyNotifyEvent_082.Instance);
            DirectConsume(in ManyNotifyEvent_082.Instance);
            DirectConsume(in ManyNotifyEvent_083.Instance);
            DirectConsume(in ManyNotifyEvent_083.Instance);
            DirectConsume(in ManyNotifyEvent_084.Instance);
            DirectConsume(in ManyNotifyEvent_084.Instance);
            DirectConsume(in ManyNotifyEvent_085.Instance);
            DirectConsume(in ManyNotifyEvent_085.Instance);
            DirectConsume(in ManyNotifyEvent_086.Instance);
            DirectConsume(in ManyNotifyEvent_086.Instance);
            DirectConsume(in ManyNotifyEvent_087.Instance);
            DirectConsume(in ManyNotifyEvent_087.Instance);
            DirectConsume(in ManyNotifyEvent_088.Instance);
            DirectConsume(in ManyNotifyEvent_088.Instance);
            DirectConsume(in ManyNotifyEvent_089.Instance);
            DirectConsume(in ManyNotifyEvent_089.Instance);
            DirectConsume(in ManyNotifyEvent_090.Instance);
            DirectConsume(in ManyNotifyEvent_090.Instance);
            DirectConsume(in ManyNotifyEvent_091.Instance);
            DirectConsume(in ManyNotifyEvent_091.Instance);
            DirectConsume(in ManyNotifyEvent_092.Instance);
            DirectConsume(in ManyNotifyEvent_092.Instance);
            DirectConsume(in ManyNotifyEvent_093.Instance);
            DirectConsume(in ManyNotifyEvent_093.Instance);
            DirectConsume(in ManyNotifyEvent_094.Instance);
            DirectConsume(in ManyNotifyEvent_094.Instance);
            DirectConsume(in ManyNotifyEvent_095.Instance);
            DirectConsume(in ManyNotifyEvent_095.Instance);
            DirectConsume(in ManyNotifyEvent_096.Instance);
            DirectConsume(in ManyNotifyEvent_096.Instance);
            DirectConsume(in ManyNotifyEvent_097.Instance);
            DirectConsume(in ManyNotifyEvent_097.Instance);
            DirectConsume(in ManyNotifyEvent_098.Instance);
            DirectConsume(in ManyNotifyEvent_098.Instance);
            DirectConsume(in ManyNotifyEvent_099.Instance);
            DirectConsume(in ManyNotifyEvent_099.Instance);
            DirectConsume(in ManyNotifyEvent_100.Instance);
            DirectConsume(in ManyNotifyEvent_100.Instance);
            DirectConsume(in ManyNotifyEvent_101.Instance);
            DirectConsume(in ManyNotifyEvent_101.Instance);
            DirectConsume(in ManyNotifyEvent_102.Instance);
            DirectConsume(in ManyNotifyEvent_102.Instance);
            DirectConsume(in ManyNotifyEvent_103.Instance);
            DirectConsume(in ManyNotifyEvent_103.Instance);
            DirectConsume(in ManyNotifyEvent_104.Instance);
            DirectConsume(in ManyNotifyEvent_104.Instance);
            DirectConsume(in ManyNotifyEvent_105.Instance);
            DirectConsume(in ManyNotifyEvent_105.Instance);
            DirectConsume(in ManyNotifyEvent_106.Instance);
            DirectConsume(in ManyNotifyEvent_106.Instance);
            DirectConsume(in ManyNotifyEvent_107.Instance);
            DirectConsume(in ManyNotifyEvent_107.Instance);
            DirectConsume(in ManyNotifyEvent_108.Instance);
            DirectConsume(in ManyNotifyEvent_108.Instance);
            DirectConsume(in ManyNotifyEvent_109.Instance);
            DirectConsume(in ManyNotifyEvent_109.Instance);
            DirectConsume(in ManyNotifyEvent_110.Instance);
            DirectConsume(in ManyNotifyEvent_110.Instance);
            DirectConsume(in ManyNotifyEvent_111.Instance);
            DirectConsume(in ManyNotifyEvent_111.Instance);
            DirectConsume(in ManyNotifyEvent_112.Instance);
            DirectConsume(in ManyNotifyEvent_112.Instance);
            DirectConsume(in ManyNotifyEvent_113.Instance);
            DirectConsume(in ManyNotifyEvent_113.Instance);
            DirectConsume(in ManyNotifyEvent_114.Instance);
            DirectConsume(in ManyNotifyEvent_114.Instance);
            DirectConsume(in ManyNotifyEvent_115.Instance);
            DirectConsume(in ManyNotifyEvent_115.Instance);
            DirectConsume(in ManyNotifyEvent_116.Instance);
            DirectConsume(in ManyNotifyEvent_116.Instance);
            DirectConsume(in ManyNotifyEvent_117.Instance);
            DirectConsume(in ManyNotifyEvent_117.Instance);
            DirectConsume(in ManyNotifyEvent_118.Instance);
            DirectConsume(in ManyNotifyEvent_118.Instance);
            DirectConsume(in ManyNotifyEvent_119.Instance);
            DirectConsume(in ManyNotifyEvent_119.Instance);
            DirectConsume(in ManyNotifyEvent_120.Instance);
            DirectConsume(in ManyNotifyEvent_120.Instance);
            DirectConsume(in ManyNotifyEvent_121.Instance);
            DirectConsume(in ManyNotifyEvent_121.Instance);
            DirectConsume(in ManyNotifyEvent_122.Instance);
            DirectConsume(in ManyNotifyEvent_122.Instance);
            DirectConsume(in ManyNotifyEvent_123.Instance);
            DirectConsume(in ManyNotifyEvent_123.Instance);
            DirectConsume(in ManyNotifyEvent_124.Instance);
            DirectConsume(in ManyNotifyEvent_124.Instance);
            DirectConsume(in ManyNotifyEvent_125.Instance);
            DirectConsume(in ManyNotifyEvent_125.Instance);
            DirectConsume(in ManyNotifyEvent_126.Instance);
            DirectConsume(in ManyNotifyEvent_126.Instance);
            DirectConsume(in ManyNotifyEvent_127.Instance);
            DirectConsume(in ManyNotifyEvent_127.Instance);
            return;
        }

        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_032.Instance);
        DirectConsume(in ManyNotifyEvent_032.Instance);
        DirectConsume(in ManyNotifyEvent_032.Instance);
        DirectConsume(in ManyNotifyEvent_033.Instance);
        DirectConsume(in ManyNotifyEvent_033.Instance);
        DirectConsume(in ManyNotifyEvent_033.Instance);
        DirectConsume(in ManyNotifyEvent_034.Instance);
        DirectConsume(in ManyNotifyEvent_034.Instance);
        DirectConsume(in ManyNotifyEvent_034.Instance);
        DirectConsume(in ManyNotifyEvent_035.Instance);
        DirectConsume(in ManyNotifyEvent_035.Instance);
        DirectConsume(in ManyNotifyEvent_035.Instance);
        DirectConsume(in ManyNotifyEvent_036.Instance);
        DirectConsume(in ManyNotifyEvent_036.Instance);
        DirectConsume(in ManyNotifyEvent_036.Instance);
        DirectConsume(in ManyNotifyEvent_037.Instance);
        DirectConsume(in ManyNotifyEvent_037.Instance);
        DirectConsume(in ManyNotifyEvent_037.Instance);
        DirectConsume(in ManyNotifyEvent_038.Instance);
        DirectConsume(in ManyNotifyEvent_038.Instance);
        DirectConsume(in ManyNotifyEvent_038.Instance);
        DirectConsume(in ManyNotifyEvent_039.Instance);
        DirectConsume(in ManyNotifyEvent_039.Instance);
        DirectConsume(in ManyNotifyEvent_039.Instance);
        DirectConsume(in ManyNotifyEvent_040.Instance);
        DirectConsume(in ManyNotifyEvent_040.Instance);
        DirectConsume(in ManyNotifyEvent_040.Instance);
        DirectConsume(in ManyNotifyEvent_041.Instance);
        DirectConsume(in ManyNotifyEvent_041.Instance);
        DirectConsume(in ManyNotifyEvent_041.Instance);
        DirectConsume(in ManyNotifyEvent_042.Instance);
        DirectConsume(in ManyNotifyEvent_042.Instance);
        DirectConsume(in ManyNotifyEvent_042.Instance);
        DirectConsume(in ManyNotifyEvent_043.Instance);
        DirectConsume(in ManyNotifyEvent_043.Instance);
        DirectConsume(in ManyNotifyEvent_043.Instance);
        DirectConsume(in ManyNotifyEvent_044.Instance);
        DirectConsume(in ManyNotifyEvent_044.Instance);
        DirectConsume(in ManyNotifyEvent_044.Instance);
        DirectConsume(in ManyNotifyEvent_045.Instance);
        DirectConsume(in ManyNotifyEvent_045.Instance);
        DirectConsume(in ManyNotifyEvent_045.Instance);
        DirectConsume(in ManyNotifyEvent_046.Instance);
        DirectConsume(in ManyNotifyEvent_046.Instance);
        DirectConsume(in ManyNotifyEvent_046.Instance);
        DirectConsume(in ManyNotifyEvent_047.Instance);
        DirectConsume(in ManyNotifyEvent_047.Instance);
        DirectConsume(in ManyNotifyEvent_047.Instance);
        DirectConsume(in ManyNotifyEvent_048.Instance);
        DirectConsume(in ManyNotifyEvent_048.Instance);
        DirectConsume(in ManyNotifyEvent_048.Instance);
        DirectConsume(in ManyNotifyEvent_049.Instance);
        DirectConsume(in ManyNotifyEvent_049.Instance);
        DirectConsume(in ManyNotifyEvent_049.Instance);
        DirectConsume(in ManyNotifyEvent_050.Instance);
        DirectConsume(in ManyNotifyEvent_050.Instance);
        DirectConsume(in ManyNotifyEvent_050.Instance);
        DirectConsume(in ManyNotifyEvent_051.Instance);
        DirectConsume(in ManyNotifyEvent_051.Instance);
        DirectConsume(in ManyNotifyEvent_051.Instance);
        DirectConsume(in ManyNotifyEvent_052.Instance);
        DirectConsume(in ManyNotifyEvent_052.Instance);
        DirectConsume(in ManyNotifyEvent_052.Instance);
        DirectConsume(in ManyNotifyEvent_053.Instance);
        DirectConsume(in ManyNotifyEvent_053.Instance);
        DirectConsume(in ManyNotifyEvent_053.Instance);
        DirectConsume(in ManyNotifyEvent_054.Instance);
        DirectConsume(in ManyNotifyEvent_054.Instance);
        DirectConsume(in ManyNotifyEvent_054.Instance);
        DirectConsume(in ManyNotifyEvent_055.Instance);
        DirectConsume(in ManyNotifyEvent_055.Instance);
        DirectConsume(in ManyNotifyEvent_055.Instance);
        DirectConsume(in ManyNotifyEvent_056.Instance);
        DirectConsume(in ManyNotifyEvent_056.Instance);
        DirectConsume(in ManyNotifyEvent_056.Instance);
        DirectConsume(in ManyNotifyEvent_057.Instance);
        DirectConsume(in ManyNotifyEvent_057.Instance);
        DirectConsume(in ManyNotifyEvent_057.Instance);
        DirectConsume(in ManyNotifyEvent_058.Instance);
        DirectConsume(in ManyNotifyEvent_058.Instance);
        DirectConsume(in ManyNotifyEvent_058.Instance);
        DirectConsume(in ManyNotifyEvent_059.Instance);
        DirectConsume(in ManyNotifyEvent_059.Instance);
        DirectConsume(in ManyNotifyEvent_059.Instance);
        DirectConsume(in ManyNotifyEvent_060.Instance);
        DirectConsume(in ManyNotifyEvent_060.Instance);
        DirectConsume(in ManyNotifyEvent_060.Instance);
        DirectConsume(in ManyNotifyEvent_061.Instance);
        DirectConsume(in ManyNotifyEvent_061.Instance);
        DirectConsume(in ManyNotifyEvent_061.Instance);
        DirectConsume(in ManyNotifyEvent_062.Instance);
        DirectConsume(in ManyNotifyEvent_062.Instance);
        DirectConsume(in ManyNotifyEvent_062.Instance);
        DirectConsume(in ManyNotifyEvent_063.Instance);
        DirectConsume(in ManyNotifyEvent_063.Instance);
        DirectConsume(in ManyNotifyEvent_063.Instance);
        DirectConsume(in ManyNotifyEvent_064.Instance);
        DirectConsume(in ManyNotifyEvent_064.Instance);
        DirectConsume(in ManyNotifyEvent_064.Instance);
        DirectConsume(in ManyNotifyEvent_065.Instance);
        DirectConsume(in ManyNotifyEvent_065.Instance);
        DirectConsume(in ManyNotifyEvent_065.Instance);
        DirectConsume(in ManyNotifyEvent_066.Instance);
        DirectConsume(in ManyNotifyEvent_066.Instance);
        DirectConsume(in ManyNotifyEvent_066.Instance);
        DirectConsume(in ManyNotifyEvent_067.Instance);
        DirectConsume(in ManyNotifyEvent_067.Instance);
        DirectConsume(in ManyNotifyEvent_067.Instance);
        DirectConsume(in ManyNotifyEvent_068.Instance);
        DirectConsume(in ManyNotifyEvent_068.Instance);
        DirectConsume(in ManyNotifyEvent_068.Instance);
        DirectConsume(in ManyNotifyEvent_069.Instance);
        DirectConsume(in ManyNotifyEvent_069.Instance);
        DirectConsume(in ManyNotifyEvent_069.Instance);
        DirectConsume(in ManyNotifyEvent_070.Instance);
        DirectConsume(in ManyNotifyEvent_070.Instance);
        DirectConsume(in ManyNotifyEvent_070.Instance);
        DirectConsume(in ManyNotifyEvent_071.Instance);
        DirectConsume(in ManyNotifyEvent_071.Instance);
        DirectConsume(in ManyNotifyEvent_071.Instance);
        DirectConsume(in ManyNotifyEvent_072.Instance);
        DirectConsume(in ManyNotifyEvent_072.Instance);
        DirectConsume(in ManyNotifyEvent_072.Instance);
        DirectConsume(in ManyNotifyEvent_073.Instance);
        DirectConsume(in ManyNotifyEvent_073.Instance);
        DirectConsume(in ManyNotifyEvent_073.Instance);
        DirectConsume(in ManyNotifyEvent_074.Instance);
        DirectConsume(in ManyNotifyEvent_074.Instance);
        DirectConsume(in ManyNotifyEvent_074.Instance);
        DirectConsume(in ManyNotifyEvent_075.Instance);
        DirectConsume(in ManyNotifyEvent_075.Instance);
        DirectConsume(in ManyNotifyEvent_075.Instance);
        DirectConsume(in ManyNotifyEvent_076.Instance);
        DirectConsume(in ManyNotifyEvent_076.Instance);
        DirectConsume(in ManyNotifyEvent_076.Instance);
        DirectConsume(in ManyNotifyEvent_077.Instance);
        DirectConsume(in ManyNotifyEvent_077.Instance);
        DirectConsume(in ManyNotifyEvent_077.Instance);
        DirectConsume(in ManyNotifyEvent_078.Instance);
        DirectConsume(in ManyNotifyEvent_078.Instance);
        DirectConsume(in ManyNotifyEvent_078.Instance);
        DirectConsume(in ManyNotifyEvent_079.Instance);
        DirectConsume(in ManyNotifyEvent_079.Instance);
        DirectConsume(in ManyNotifyEvent_079.Instance);
        DirectConsume(in ManyNotifyEvent_080.Instance);
        DirectConsume(in ManyNotifyEvent_080.Instance);
        DirectConsume(in ManyNotifyEvent_080.Instance);
        DirectConsume(in ManyNotifyEvent_081.Instance);
        DirectConsume(in ManyNotifyEvent_081.Instance);
        DirectConsume(in ManyNotifyEvent_081.Instance);
        DirectConsume(in ManyNotifyEvent_082.Instance);
        DirectConsume(in ManyNotifyEvent_082.Instance);
        DirectConsume(in ManyNotifyEvent_082.Instance);
        DirectConsume(in ManyNotifyEvent_083.Instance);
        DirectConsume(in ManyNotifyEvent_083.Instance);
        DirectConsume(in ManyNotifyEvent_083.Instance);
        DirectConsume(in ManyNotifyEvent_084.Instance);
        DirectConsume(in ManyNotifyEvent_084.Instance);
        DirectConsume(in ManyNotifyEvent_084.Instance);
        DirectConsume(in ManyNotifyEvent_085.Instance);
        DirectConsume(in ManyNotifyEvent_085.Instance);
        DirectConsume(in ManyNotifyEvent_085.Instance);
        DirectConsume(in ManyNotifyEvent_086.Instance);
        DirectConsume(in ManyNotifyEvent_086.Instance);
        DirectConsume(in ManyNotifyEvent_086.Instance);
        DirectConsume(in ManyNotifyEvent_087.Instance);
        DirectConsume(in ManyNotifyEvent_087.Instance);
        DirectConsume(in ManyNotifyEvent_087.Instance);
        DirectConsume(in ManyNotifyEvent_088.Instance);
        DirectConsume(in ManyNotifyEvent_088.Instance);
        DirectConsume(in ManyNotifyEvent_088.Instance);
        DirectConsume(in ManyNotifyEvent_089.Instance);
        DirectConsume(in ManyNotifyEvent_089.Instance);
        DirectConsume(in ManyNotifyEvent_089.Instance);
        DirectConsume(in ManyNotifyEvent_090.Instance);
        DirectConsume(in ManyNotifyEvent_090.Instance);
        DirectConsume(in ManyNotifyEvent_090.Instance);
        DirectConsume(in ManyNotifyEvent_091.Instance);
        DirectConsume(in ManyNotifyEvent_091.Instance);
        DirectConsume(in ManyNotifyEvent_091.Instance);
        DirectConsume(in ManyNotifyEvent_092.Instance);
        DirectConsume(in ManyNotifyEvent_092.Instance);
        DirectConsume(in ManyNotifyEvent_092.Instance);
        DirectConsume(in ManyNotifyEvent_093.Instance);
        DirectConsume(in ManyNotifyEvent_093.Instance);
        DirectConsume(in ManyNotifyEvent_093.Instance);
        DirectConsume(in ManyNotifyEvent_094.Instance);
        DirectConsume(in ManyNotifyEvent_094.Instance);
        DirectConsume(in ManyNotifyEvent_094.Instance);
        DirectConsume(in ManyNotifyEvent_095.Instance);
        DirectConsume(in ManyNotifyEvent_095.Instance);
        DirectConsume(in ManyNotifyEvent_095.Instance);
        DirectConsume(in ManyNotifyEvent_096.Instance);
        DirectConsume(in ManyNotifyEvent_096.Instance);
        DirectConsume(in ManyNotifyEvent_096.Instance);
        DirectConsume(in ManyNotifyEvent_097.Instance);
        DirectConsume(in ManyNotifyEvent_097.Instance);
        DirectConsume(in ManyNotifyEvent_097.Instance);
        DirectConsume(in ManyNotifyEvent_098.Instance);
        DirectConsume(in ManyNotifyEvent_098.Instance);
        DirectConsume(in ManyNotifyEvent_098.Instance);
        DirectConsume(in ManyNotifyEvent_099.Instance);
        DirectConsume(in ManyNotifyEvent_099.Instance);
        DirectConsume(in ManyNotifyEvent_099.Instance);
        DirectConsume(in ManyNotifyEvent_100.Instance);
        DirectConsume(in ManyNotifyEvent_100.Instance);
        DirectConsume(in ManyNotifyEvent_100.Instance);
        DirectConsume(in ManyNotifyEvent_101.Instance);
        DirectConsume(in ManyNotifyEvent_101.Instance);
        DirectConsume(in ManyNotifyEvent_101.Instance);
        DirectConsume(in ManyNotifyEvent_102.Instance);
        DirectConsume(in ManyNotifyEvent_102.Instance);
        DirectConsume(in ManyNotifyEvent_102.Instance);
        DirectConsume(in ManyNotifyEvent_103.Instance);
        DirectConsume(in ManyNotifyEvent_103.Instance);
        DirectConsume(in ManyNotifyEvent_103.Instance);
        DirectConsume(in ManyNotifyEvent_104.Instance);
        DirectConsume(in ManyNotifyEvent_104.Instance);
        DirectConsume(in ManyNotifyEvent_104.Instance);
        DirectConsume(in ManyNotifyEvent_105.Instance);
        DirectConsume(in ManyNotifyEvent_105.Instance);
        DirectConsume(in ManyNotifyEvent_105.Instance);
        DirectConsume(in ManyNotifyEvent_106.Instance);
        DirectConsume(in ManyNotifyEvent_106.Instance);
        DirectConsume(in ManyNotifyEvent_106.Instance);
        DirectConsume(in ManyNotifyEvent_107.Instance);
        DirectConsume(in ManyNotifyEvent_107.Instance);
        DirectConsume(in ManyNotifyEvent_107.Instance);
        DirectConsume(in ManyNotifyEvent_108.Instance);
        DirectConsume(in ManyNotifyEvent_108.Instance);
        DirectConsume(in ManyNotifyEvent_108.Instance);
        DirectConsume(in ManyNotifyEvent_109.Instance);
        DirectConsume(in ManyNotifyEvent_109.Instance);
        DirectConsume(in ManyNotifyEvent_109.Instance);
        DirectConsume(in ManyNotifyEvent_110.Instance);
        DirectConsume(in ManyNotifyEvent_110.Instance);
        DirectConsume(in ManyNotifyEvent_110.Instance);
        DirectConsume(in ManyNotifyEvent_111.Instance);
        DirectConsume(in ManyNotifyEvent_111.Instance);
        DirectConsume(in ManyNotifyEvent_111.Instance);
        DirectConsume(in ManyNotifyEvent_112.Instance);
        DirectConsume(in ManyNotifyEvent_112.Instance);
        DirectConsume(in ManyNotifyEvent_112.Instance);
        DirectConsume(in ManyNotifyEvent_113.Instance);
        DirectConsume(in ManyNotifyEvent_113.Instance);
        DirectConsume(in ManyNotifyEvent_113.Instance);
        DirectConsume(in ManyNotifyEvent_114.Instance);
        DirectConsume(in ManyNotifyEvent_114.Instance);
        DirectConsume(in ManyNotifyEvent_114.Instance);
        DirectConsume(in ManyNotifyEvent_115.Instance);
        DirectConsume(in ManyNotifyEvent_115.Instance);
        DirectConsume(in ManyNotifyEvent_115.Instance);
        DirectConsume(in ManyNotifyEvent_116.Instance);
        DirectConsume(in ManyNotifyEvent_116.Instance);
        DirectConsume(in ManyNotifyEvent_116.Instance);
        DirectConsume(in ManyNotifyEvent_117.Instance);
        DirectConsume(in ManyNotifyEvent_117.Instance);
        DirectConsume(in ManyNotifyEvent_117.Instance);
        DirectConsume(in ManyNotifyEvent_118.Instance);
        DirectConsume(in ManyNotifyEvent_118.Instance);
        DirectConsume(in ManyNotifyEvent_118.Instance);
        DirectConsume(in ManyNotifyEvent_119.Instance);
        DirectConsume(in ManyNotifyEvent_119.Instance);
        DirectConsume(in ManyNotifyEvent_119.Instance);
        DirectConsume(in ManyNotifyEvent_120.Instance);
        DirectConsume(in ManyNotifyEvent_120.Instance);
        DirectConsume(in ManyNotifyEvent_120.Instance);
        DirectConsume(in ManyNotifyEvent_121.Instance);
        DirectConsume(in ManyNotifyEvent_121.Instance);
        DirectConsume(in ManyNotifyEvent_121.Instance);
        DirectConsume(in ManyNotifyEvent_122.Instance);
        DirectConsume(in ManyNotifyEvent_122.Instance);
        DirectConsume(in ManyNotifyEvent_122.Instance);
        DirectConsume(in ManyNotifyEvent_123.Instance);
        DirectConsume(in ManyNotifyEvent_123.Instance);
        DirectConsume(in ManyNotifyEvent_123.Instance);
        DirectConsume(in ManyNotifyEvent_124.Instance);
        DirectConsume(in ManyNotifyEvent_124.Instance);
        DirectConsume(in ManyNotifyEvent_124.Instance);
        DirectConsume(in ManyNotifyEvent_125.Instance);
        DirectConsume(in ManyNotifyEvent_125.Instance);
        DirectConsume(in ManyNotifyEvent_125.Instance);
        DirectConsume(in ManyNotifyEvent_126.Instance);
        DirectConsume(in ManyNotifyEvent_126.Instance);
        DirectConsume(in ManyNotifyEvent_126.Instance);
        DirectConsume(in ManyNotifyEvent_127.Instance);
        DirectConsume(in ManyNotifyEvent_127.Instance);
        DirectConsume(in ManyNotifyEvent_127.Instance);
    }

    public static void DispatchLayerBase128()
    {
        LayerHub.Send(ManyNotifyEvent_000.Instance);
        LayerHub.Send(ManyNotifyEvent_001.Instance);
        LayerHub.Send(ManyNotifyEvent_002.Instance);
        LayerHub.Send(ManyNotifyEvent_003.Instance);
        LayerHub.Send(ManyNotifyEvent_004.Instance);
        LayerHub.Send(ManyNotifyEvent_005.Instance);
        LayerHub.Send(ManyNotifyEvent_006.Instance);
        LayerHub.Send(ManyNotifyEvent_007.Instance);
        LayerHub.Send(ManyNotifyEvent_008.Instance);
        LayerHub.Send(ManyNotifyEvent_009.Instance);
        LayerHub.Send(ManyNotifyEvent_010.Instance);
        LayerHub.Send(ManyNotifyEvent_011.Instance);
        LayerHub.Send(ManyNotifyEvent_012.Instance);
        LayerHub.Send(ManyNotifyEvent_013.Instance);
        LayerHub.Send(ManyNotifyEvent_014.Instance);
        LayerHub.Send(ManyNotifyEvent_015.Instance);
        LayerHub.Send(ManyNotifyEvent_016.Instance);
        LayerHub.Send(ManyNotifyEvent_017.Instance);
        LayerHub.Send(ManyNotifyEvent_018.Instance);
        LayerHub.Send(ManyNotifyEvent_019.Instance);
        LayerHub.Send(ManyNotifyEvent_020.Instance);
        LayerHub.Send(ManyNotifyEvent_021.Instance);
        LayerHub.Send(ManyNotifyEvent_022.Instance);
        LayerHub.Send(ManyNotifyEvent_023.Instance);
        LayerHub.Send(ManyNotifyEvent_024.Instance);
        LayerHub.Send(ManyNotifyEvent_025.Instance);
        LayerHub.Send(ManyNotifyEvent_026.Instance);
        LayerHub.Send(ManyNotifyEvent_027.Instance);
        LayerHub.Send(ManyNotifyEvent_028.Instance);
        LayerHub.Send(ManyNotifyEvent_029.Instance);
        LayerHub.Send(ManyNotifyEvent_030.Instance);
        LayerHub.Send(ManyNotifyEvent_031.Instance);
        LayerHub.Send(ManyNotifyEvent_032.Instance);
        LayerHub.Send(ManyNotifyEvent_033.Instance);
        LayerHub.Send(ManyNotifyEvent_034.Instance);
        LayerHub.Send(ManyNotifyEvent_035.Instance);
        LayerHub.Send(ManyNotifyEvent_036.Instance);
        LayerHub.Send(ManyNotifyEvent_037.Instance);
        LayerHub.Send(ManyNotifyEvent_038.Instance);
        LayerHub.Send(ManyNotifyEvent_039.Instance);
        LayerHub.Send(ManyNotifyEvent_040.Instance);
        LayerHub.Send(ManyNotifyEvent_041.Instance);
        LayerHub.Send(ManyNotifyEvent_042.Instance);
        LayerHub.Send(ManyNotifyEvent_043.Instance);
        LayerHub.Send(ManyNotifyEvent_044.Instance);
        LayerHub.Send(ManyNotifyEvent_045.Instance);
        LayerHub.Send(ManyNotifyEvent_046.Instance);
        LayerHub.Send(ManyNotifyEvent_047.Instance);
        LayerHub.Send(ManyNotifyEvent_048.Instance);
        LayerHub.Send(ManyNotifyEvent_049.Instance);
        LayerHub.Send(ManyNotifyEvent_050.Instance);
        LayerHub.Send(ManyNotifyEvent_051.Instance);
        LayerHub.Send(ManyNotifyEvent_052.Instance);
        LayerHub.Send(ManyNotifyEvent_053.Instance);
        LayerHub.Send(ManyNotifyEvent_054.Instance);
        LayerHub.Send(ManyNotifyEvent_055.Instance);
        LayerHub.Send(ManyNotifyEvent_056.Instance);
        LayerHub.Send(ManyNotifyEvent_057.Instance);
        LayerHub.Send(ManyNotifyEvent_058.Instance);
        LayerHub.Send(ManyNotifyEvent_059.Instance);
        LayerHub.Send(ManyNotifyEvent_060.Instance);
        LayerHub.Send(ManyNotifyEvent_061.Instance);
        LayerHub.Send(ManyNotifyEvent_062.Instance);
        LayerHub.Send(ManyNotifyEvent_063.Instance);
        LayerHub.Send(ManyNotifyEvent_064.Instance);
        LayerHub.Send(ManyNotifyEvent_065.Instance);
        LayerHub.Send(ManyNotifyEvent_066.Instance);
        LayerHub.Send(ManyNotifyEvent_067.Instance);
        LayerHub.Send(ManyNotifyEvent_068.Instance);
        LayerHub.Send(ManyNotifyEvent_069.Instance);
        LayerHub.Send(ManyNotifyEvent_070.Instance);
        LayerHub.Send(ManyNotifyEvent_071.Instance);
        LayerHub.Send(ManyNotifyEvent_072.Instance);
        LayerHub.Send(ManyNotifyEvent_073.Instance);
        LayerHub.Send(ManyNotifyEvent_074.Instance);
        LayerHub.Send(ManyNotifyEvent_075.Instance);
        LayerHub.Send(ManyNotifyEvent_076.Instance);
        LayerHub.Send(ManyNotifyEvent_077.Instance);
        LayerHub.Send(ManyNotifyEvent_078.Instance);
        LayerHub.Send(ManyNotifyEvent_079.Instance);
        LayerHub.Send(ManyNotifyEvent_080.Instance);
        LayerHub.Send(ManyNotifyEvent_081.Instance);
        LayerHub.Send(ManyNotifyEvent_082.Instance);
        LayerHub.Send(ManyNotifyEvent_083.Instance);
        LayerHub.Send(ManyNotifyEvent_084.Instance);
        LayerHub.Send(ManyNotifyEvent_085.Instance);
        LayerHub.Send(ManyNotifyEvent_086.Instance);
        LayerHub.Send(ManyNotifyEvent_087.Instance);
        LayerHub.Send(ManyNotifyEvent_088.Instance);
        LayerHub.Send(ManyNotifyEvent_089.Instance);
        LayerHub.Send(ManyNotifyEvent_090.Instance);
        LayerHub.Send(ManyNotifyEvent_091.Instance);
        LayerHub.Send(ManyNotifyEvent_092.Instance);
        LayerHub.Send(ManyNotifyEvent_093.Instance);
        LayerHub.Send(ManyNotifyEvent_094.Instance);
        LayerHub.Send(ManyNotifyEvent_095.Instance);
        LayerHub.Send(ManyNotifyEvent_096.Instance);
        LayerHub.Send(ManyNotifyEvent_097.Instance);
        LayerHub.Send(ManyNotifyEvent_098.Instance);
        LayerHub.Send(ManyNotifyEvent_099.Instance);
        LayerHub.Send(ManyNotifyEvent_100.Instance);
        LayerHub.Send(ManyNotifyEvent_101.Instance);
        LayerHub.Send(ManyNotifyEvent_102.Instance);
        LayerHub.Send(ManyNotifyEvent_103.Instance);
        LayerHub.Send(ManyNotifyEvent_104.Instance);
        LayerHub.Send(ManyNotifyEvent_105.Instance);
        LayerHub.Send(ManyNotifyEvent_106.Instance);
        LayerHub.Send(ManyNotifyEvent_107.Instance);
        LayerHub.Send(ManyNotifyEvent_108.Instance);
        LayerHub.Send(ManyNotifyEvent_109.Instance);
        LayerHub.Send(ManyNotifyEvent_110.Instance);
        LayerHub.Send(ManyNotifyEvent_111.Instance);
        LayerHub.Send(ManyNotifyEvent_112.Instance);
        LayerHub.Send(ManyNotifyEvent_113.Instance);
        LayerHub.Send(ManyNotifyEvent_114.Instance);
        LayerHub.Send(ManyNotifyEvent_115.Instance);
        LayerHub.Send(ManyNotifyEvent_116.Instance);
        LayerHub.Send(ManyNotifyEvent_117.Instance);
        LayerHub.Send(ManyNotifyEvent_118.Instance);
        LayerHub.Send(ManyNotifyEvent_119.Instance);
        LayerHub.Send(ManyNotifyEvent_120.Instance);
        LayerHub.Send(ManyNotifyEvent_121.Instance);
        LayerHub.Send(ManyNotifyEvent_122.Instance);
        LayerHub.Send(ManyNotifyEvent_123.Instance);
        LayerHub.Send(ManyNotifyEvent_124.Instance);
        LayerHub.Send(ManyNotifyEvent_125.Instance);
        LayerHub.Send(ManyNotifyEvent_126.Instance);
        LayerHub.Send(ManyNotifyEvent_127.Instance);
    }

    public static void DispatchMessagePipe128(ManyNotifyBatch128Publishers publishers)
    {
        publishers.P000.Publish(ManyNotifyEvent_000.Instance);
        publishers.P001.Publish(ManyNotifyEvent_001.Instance);
        publishers.P002.Publish(ManyNotifyEvent_002.Instance);
        publishers.P003.Publish(ManyNotifyEvent_003.Instance);
        publishers.P004.Publish(ManyNotifyEvent_004.Instance);
        publishers.P005.Publish(ManyNotifyEvent_005.Instance);
        publishers.P006.Publish(ManyNotifyEvent_006.Instance);
        publishers.P007.Publish(ManyNotifyEvent_007.Instance);
        publishers.P008.Publish(ManyNotifyEvent_008.Instance);
        publishers.P009.Publish(ManyNotifyEvent_009.Instance);
        publishers.P010.Publish(ManyNotifyEvent_010.Instance);
        publishers.P011.Publish(ManyNotifyEvent_011.Instance);
        publishers.P012.Publish(ManyNotifyEvent_012.Instance);
        publishers.P013.Publish(ManyNotifyEvent_013.Instance);
        publishers.P014.Publish(ManyNotifyEvent_014.Instance);
        publishers.P015.Publish(ManyNotifyEvent_015.Instance);
        publishers.P016.Publish(ManyNotifyEvent_016.Instance);
        publishers.P017.Publish(ManyNotifyEvent_017.Instance);
        publishers.P018.Publish(ManyNotifyEvent_018.Instance);
        publishers.P019.Publish(ManyNotifyEvent_019.Instance);
        publishers.P020.Publish(ManyNotifyEvent_020.Instance);
        publishers.P021.Publish(ManyNotifyEvent_021.Instance);
        publishers.P022.Publish(ManyNotifyEvent_022.Instance);
        publishers.P023.Publish(ManyNotifyEvent_023.Instance);
        publishers.P024.Publish(ManyNotifyEvent_024.Instance);
        publishers.P025.Publish(ManyNotifyEvent_025.Instance);
        publishers.P026.Publish(ManyNotifyEvent_026.Instance);
        publishers.P027.Publish(ManyNotifyEvent_027.Instance);
        publishers.P028.Publish(ManyNotifyEvent_028.Instance);
        publishers.P029.Publish(ManyNotifyEvent_029.Instance);
        publishers.P030.Publish(ManyNotifyEvent_030.Instance);
        publishers.P031.Publish(ManyNotifyEvent_031.Instance);
        publishers.P032.Publish(ManyNotifyEvent_032.Instance);
        publishers.P033.Publish(ManyNotifyEvent_033.Instance);
        publishers.P034.Publish(ManyNotifyEvent_034.Instance);
        publishers.P035.Publish(ManyNotifyEvent_035.Instance);
        publishers.P036.Publish(ManyNotifyEvent_036.Instance);
        publishers.P037.Publish(ManyNotifyEvent_037.Instance);
        publishers.P038.Publish(ManyNotifyEvent_038.Instance);
        publishers.P039.Publish(ManyNotifyEvent_039.Instance);
        publishers.P040.Publish(ManyNotifyEvent_040.Instance);
        publishers.P041.Publish(ManyNotifyEvent_041.Instance);
        publishers.P042.Publish(ManyNotifyEvent_042.Instance);
        publishers.P043.Publish(ManyNotifyEvent_043.Instance);
        publishers.P044.Publish(ManyNotifyEvent_044.Instance);
        publishers.P045.Publish(ManyNotifyEvent_045.Instance);
        publishers.P046.Publish(ManyNotifyEvent_046.Instance);
        publishers.P047.Publish(ManyNotifyEvent_047.Instance);
        publishers.P048.Publish(ManyNotifyEvent_048.Instance);
        publishers.P049.Publish(ManyNotifyEvent_049.Instance);
        publishers.P050.Publish(ManyNotifyEvent_050.Instance);
        publishers.P051.Publish(ManyNotifyEvent_051.Instance);
        publishers.P052.Publish(ManyNotifyEvent_052.Instance);
        publishers.P053.Publish(ManyNotifyEvent_053.Instance);
        publishers.P054.Publish(ManyNotifyEvent_054.Instance);
        publishers.P055.Publish(ManyNotifyEvent_055.Instance);
        publishers.P056.Publish(ManyNotifyEvent_056.Instance);
        publishers.P057.Publish(ManyNotifyEvent_057.Instance);
        publishers.P058.Publish(ManyNotifyEvent_058.Instance);
        publishers.P059.Publish(ManyNotifyEvent_059.Instance);
        publishers.P060.Publish(ManyNotifyEvent_060.Instance);
        publishers.P061.Publish(ManyNotifyEvent_061.Instance);
        publishers.P062.Publish(ManyNotifyEvent_062.Instance);
        publishers.P063.Publish(ManyNotifyEvent_063.Instance);
        publishers.P064.Publish(ManyNotifyEvent_064.Instance);
        publishers.P065.Publish(ManyNotifyEvent_065.Instance);
        publishers.P066.Publish(ManyNotifyEvent_066.Instance);
        publishers.P067.Publish(ManyNotifyEvent_067.Instance);
        publishers.P068.Publish(ManyNotifyEvent_068.Instance);
        publishers.P069.Publish(ManyNotifyEvent_069.Instance);
        publishers.P070.Publish(ManyNotifyEvent_070.Instance);
        publishers.P071.Publish(ManyNotifyEvent_071.Instance);
        publishers.P072.Publish(ManyNotifyEvent_072.Instance);
        publishers.P073.Publish(ManyNotifyEvent_073.Instance);
        publishers.P074.Publish(ManyNotifyEvent_074.Instance);
        publishers.P075.Publish(ManyNotifyEvent_075.Instance);
        publishers.P076.Publish(ManyNotifyEvent_076.Instance);
        publishers.P077.Publish(ManyNotifyEvent_077.Instance);
        publishers.P078.Publish(ManyNotifyEvent_078.Instance);
        publishers.P079.Publish(ManyNotifyEvent_079.Instance);
        publishers.P080.Publish(ManyNotifyEvent_080.Instance);
        publishers.P081.Publish(ManyNotifyEvent_081.Instance);
        publishers.P082.Publish(ManyNotifyEvent_082.Instance);
        publishers.P083.Publish(ManyNotifyEvent_083.Instance);
        publishers.P084.Publish(ManyNotifyEvent_084.Instance);
        publishers.P085.Publish(ManyNotifyEvent_085.Instance);
        publishers.P086.Publish(ManyNotifyEvent_086.Instance);
        publishers.P087.Publish(ManyNotifyEvent_087.Instance);
        publishers.P088.Publish(ManyNotifyEvent_088.Instance);
        publishers.P089.Publish(ManyNotifyEvent_089.Instance);
        publishers.P090.Publish(ManyNotifyEvent_090.Instance);
        publishers.P091.Publish(ManyNotifyEvent_091.Instance);
        publishers.P092.Publish(ManyNotifyEvent_092.Instance);
        publishers.P093.Publish(ManyNotifyEvent_093.Instance);
        publishers.P094.Publish(ManyNotifyEvent_094.Instance);
        publishers.P095.Publish(ManyNotifyEvent_095.Instance);
        publishers.P096.Publish(ManyNotifyEvent_096.Instance);
        publishers.P097.Publish(ManyNotifyEvent_097.Instance);
        publishers.P098.Publish(ManyNotifyEvent_098.Instance);
        publishers.P099.Publish(ManyNotifyEvent_099.Instance);
        publishers.P100.Publish(ManyNotifyEvent_100.Instance);
        publishers.P101.Publish(ManyNotifyEvent_101.Instance);
        publishers.P102.Publish(ManyNotifyEvent_102.Instance);
        publishers.P103.Publish(ManyNotifyEvent_103.Instance);
        publishers.P104.Publish(ManyNotifyEvent_104.Instance);
        publishers.P105.Publish(ManyNotifyEvent_105.Instance);
        publishers.P106.Publish(ManyNotifyEvent_106.Instance);
        publishers.P107.Publish(ManyNotifyEvent_107.Instance);
        publishers.P108.Publish(ManyNotifyEvent_108.Instance);
        publishers.P109.Publish(ManyNotifyEvent_109.Instance);
        publishers.P110.Publish(ManyNotifyEvent_110.Instance);
        publishers.P111.Publish(ManyNotifyEvent_111.Instance);
        publishers.P112.Publish(ManyNotifyEvent_112.Instance);
        publishers.P113.Publish(ManyNotifyEvent_113.Instance);
        publishers.P114.Publish(ManyNotifyEvent_114.Instance);
        publishers.P115.Publish(ManyNotifyEvent_115.Instance);
        publishers.P116.Publish(ManyNotifyEvent_116.Instance);
        publishers.P117.Publish(ManyNotifyEvent_117.Instance);
        publishers.P118.Publish(ManyNotifyEvent_118.Instance);
        publishers.P119.Publish(ManyNotifyEvent_119.Instance);
        publishers.P120.Publish(ManyNotifyEvent_120.Instance);
        publishers.P121.Publish(ManyNotifyEvent_121.Instance);
        publishers.P122.Publish(ManyNotifyEvent_122.Instance);
        publishers.P123.Publish(ManyNotifyEvent_123.Instance);
        publishers.P124.Publish(ManyNotifyEvent_124.Instance);
        publishers.P125.Publish(ManyNotifyEvent_125.Instance);
        publishers.P126.Publish(ManyNotifyEvent_126.Instance);
        publishers.P127.Publish(ManyNotifyEvent_127.Instance);
    }

    public static void RegisterLayerBase256(CompareLayer layer, int subscribersPerEvent)
    {
        RegisterServiceCopies<ManyNotifyManager_000>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_001>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_002>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_003>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_004>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_005>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_006>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_007>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_008>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_009>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_010>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_011>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_012>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_013>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_014>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_015>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_016>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_017>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_018>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_019>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_020>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_021>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_022>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_023>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_024>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_025>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_026>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_027>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_028>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_029>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_030>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_031>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_032>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_033>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_034>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_035>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_036>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_037>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_038>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_039>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_040>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_041>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_042>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_043>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_044>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_045>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_046>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_047>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_048>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_049>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_050>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_051>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_052>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_053>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_054>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_055>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_056>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_057>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_058>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_059>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_060>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_061>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_062>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_063>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_064>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_065>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_066>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_067>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_068>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_069>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_070>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_071>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_072>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_073>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_074>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_075>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_076>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_077>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_078>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_079>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_080>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_081>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_082>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_083>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_084>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_085>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_086>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_087>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_088>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_089>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_090>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_091>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_092>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_093>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_094>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_095>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_096>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_097>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_098>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_099>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_100>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_101>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_102>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_103>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_104>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_105>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_106>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_107>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_108>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_109>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_110>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_111>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_112>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_113>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_114>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_115>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_116>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_117>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_118>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_119>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_120>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_121>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_122>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_123>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_124>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_125>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_126>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_127>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_128>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_129>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_130>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_131>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_132>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_133>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_134>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_135>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_136>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_137>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_138>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_139>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_140>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_141>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_142>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_143>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_144>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_145>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_146>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_147>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_148>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_149>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_150>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_151>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_152>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_153>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_154>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_155>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_156>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_157>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_158>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_159>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_160>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_161>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_162>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_163>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_164>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_165>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_166>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_167>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_168>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_169>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_170>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_171>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_172>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_173>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_174>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_175>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_176>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_177>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_178>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_179>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_180>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_181>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_182>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_183>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_184>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_185>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_186>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_187>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_188>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_189>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_190>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_191>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_192>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_193>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_194>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_195>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_196>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_197>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_198>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_199>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_200>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_201>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_202>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_203>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_204>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_205>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_206>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_207>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_208>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_209>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_210>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_211>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_212>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_213>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_214>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_215>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_216>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_217>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_218>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_219>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_220>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_221>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_222>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_223>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_224>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_225>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_226>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_227>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_228>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_229>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_230>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_231>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_232>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_233>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_234>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_235>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_236>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_237>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_238>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_239>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_240>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_241>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_242>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_243>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_244>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_245>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_246>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_247>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_248>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_249>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_250>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_251>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_252>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_253>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_254>(layer, subscribersPerEvent);
        RegisterServiceCopies<ManyNotifyManager_255>(layer, subscribersPerEvent);
    }

    public static ManyNotifyBatch256Publishers CreatePublishers256(IServiceProvider  provider, int subscribersPerEvent,
                                                                   List<IDisposable> subscriptions)
    {
        var publishers = new ManyNotifyBatch256Publishers();
        SubscribeCopies<ManyNotifyEvent_000>(provider, subscribersPerEvent, subscriptions);
        publishers.P000 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_000>>();
        SubscribeCopies<ManyNotifyEvent_001>(provider, subscribersPerEvent, subscriptions);
        publishers.P001 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_001>>();
        SubscribeCopies<ManyNotifyEvent_002>(provider, subscribersPerEvent, subscriptions);
        publishers.P002 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_002>>();
        SubscribeCopies<ManyNotifyEvent_003>(provider, subscribersPerEvent, subscriptions);
        publishers.P003 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_003>>();
        SubscribeCopies<ManyNotifyEvent_004>(provider, subscribersPerEvent, subscriptions);
        publishers.P004 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_004>>();
        SubscribeCopies<ManyNotifyEvent_005>(provider, subscribersPerEvent, subscriptions);
        publishers.P005 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_005>>();
        SubscribeCopies<ManyNotifyEvent_006>(provider, subscribersPerEvent, subscriptions);
        publishers.P006 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_006>>();
        SubscribeCopies<ManyNotifyEvent_007>(provider, subscribersPerEvent, subscriptions);
        publishers.P007 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_007>>();
        SubscribeCopies<ManyNotifyEvent_008>(provider, subscribersPerEvent, subscriptions);
        publishers.P008 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_008>>();
        SubscribeCopies<ManyNotifyEvent_009>(provider, subscribersPerEvent, subscriptions);
        publishers.P009 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_009>>();
        SubscribeCopies<ManyNotifyEvent_010>(provider, subscribersPerEvent, subscriptions);
        publishers.P010 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_010>>();
        SubscribeCopies<ManyNotifyEvent_011>(provider, subscribersPerEvent, subscriptions);
        publishers.P011 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_011>>();
        SubscribeCopies<ManyNotifyEvent_012>(provider, subscribersPerEvent, subscriptions);
        publishers.P012 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_012>>();
        SubscribeCopies<ManyNotifyEvent_013>(provider, subscribersPerEvent, subscriptions);
        publishers.P013 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_013>>();
        SubscribeCopies<ManyNotifyEvent_014>(provider, subscribersPerEvent, subscriptions);
        publishers.P014 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_014>>();
        SubscribeCopies<ManyNotifyEvent_015>(provider, subscribersPerEvent, subscriptions);
        publishers.P015 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_015>>();
        SubscribeCopies<ManyNotifyEvent_016>(provider, subscribersPerEvent, subscriptions);
        publishers.P016 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_016>>();
        SubscribeCopies<ManyNotifyEvent_017>(provider, subscribersPerEvent, subscriptions);
        publishers.P017 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_017>>();
        SubscribeCopies<ManyNotifyEvent_018>(provider, subscribersPerEvent, subscriptions);
        publishers.P018 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_018>>();
        SubscribeCopies<ManyNotifyEvent_019>(provider, subscribersPerEvent, subscriptions);
        publishers.P019 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_019>>();
        SubscribeCopies<ManyNotifyEvent_020>(provider, subscribersPerEvent, subscriptions);
        publishers.P020 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_020>>();
        SubscribeCopies<ManyNotifyEvent_021>(provider, subscribersPerEvent, subscriptions);
        publishers.P021 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_021>>();
        SubscribeCopies<ManyNotifyEvent_022>(provider, subscribersPerEvent, subscriptions);
        publishers.P022 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_022>>();
        SubscribeCopies<ManyNotifyEvent_023>(provider, subscribersPerEvent, subscriptions);
        publishers.P023 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_023>>();
        SubscribeCopies<ManyNotifyEvent_024>(provider, subscribersPerEvent, subscriptions);
        publishers.P024 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_024>>();
        SubscribeCopies<ManyNotifyEvent_025>(provider, subscribersPerEvent, subscriptions);
        publishers.P025 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_025>>();
        SubscribeCopies<ManyNotifyEvent_026>(provider, subscribersPerEvent, subscriptions);
        publishers.P026 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_026>>();
        SubscribeCopies<ManyNotifyEvent_027>(provider, subscribersPerEvent, subscriptions);
        publishers.P027 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_027>>();
        SubscribeCopies<ManyNotifyEvent_028>(provider, subscribersPerEvent, subscriptions);
        publishers.P028 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_028>>();
        SubscribeCopies<ManyNotifyEvent_029>(provider, subscribersPerEvent, subscriptions);
        publishers.P029 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_029>>();
        SubscribeCopies<ManyNotifyEvent_030>(provider, subscribersPerEvent, subscriptions);
        publishers.P030 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_030>>();
        SubscribeCopies<ManyNotifyEvent_031>(provider, subscribersPerEvent, subscriptions);
        publishers.P031 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_031>>();
        SubscribeCopies<ManyNotifyEvent_032>(provider, subscribersPerEvent, subscriptions);
        publishers.P032 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_032>>();
        SubscribeCopies<ManyNotifyEvent_033>(provider, subscribersPerEvent, subscriptions);
        publishers.P033 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_033>>();
        SubscribeCopies<ManyNotifyEvent_034>(provider, subscribersPerEvent, subscriptions);
        publishers.P034 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_034>>();
        SubscribeCopies<ManyNotifyEvent_035>(provider, subscribersPerEvent, subscriptions);
        publishers.P035 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_035>>();
        SubscribeCopies<ManyNotifyEvent_036>(provider, subscribersPerEvent, subscriptions);
        publishers.P036 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_036>>();
        SubscribeCopies<ManyNotifyEvent_037>(provider, subscribersPerEvent, subscriptions);
        publishers.P037 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_037>>();
        SubscribeCopies<ManyNotifyEvent_038>(provider, subscribersPerEvent, subscriptions);
        publishers.P038 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_038>>();
        SubscribeCopies<ManyNotifyEvent_039>(provider, subscribersPerEvent, subscriptions);
        publishers.P039 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_039>>();
        SubscribeCopies<ManyNotifyEvent_040>(provider, subscribersPerEvent, subscriptions);
        publishers.P040 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_040>>();
        SubscribeCopies<ManyNotifyEvent_041>(provider, subscribersPerEvent, subscriptions);
        publishers.P041 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_041>>();
        SubscribeCopies<ManyNotifyEvent_042>(provider, subscribersPerEvent, subscriptions);
        publishers.P042 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_042>>();
        SubscribeCopies<ManyNotifyEvent_043>(provider, subscribersPerEvent, subscriptions);
        publishers.P043 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_043>>();
        SubscribeCopies<ManyNotifyEvent_044>(provider, subscribersPerEvent, subscriptions);
        publishers.P044 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_044>>();
        SubscribeCopies<ManyNotifyEvent_045>(provider, subscribersPerEvent, subscriptions);
        publishers.P045 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_045>>();
        SubscribeCopies<ManyNotifyEvent_046>(provider, subscribersPerEvent, subscriptions);
        publishers.P046 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_046>>();
        SubscribeCopies<ManyNotifyEvent_047>(provider, subscribersPerEvent, subscriptions);
        publishers.P047 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_047>>();
        SubscribeCopies<ManyNotifyEvent_048>(provider, subscribersPerEvent, subscriptions);
        publishers.P048 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_048>>();
        SubscribeCopies<ManyNotifyEvent_049>(provider, subscribersPerEvent, subscriptions);
        publishers.P049 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_049>>();
        SubscribeCopies<ManyNotifyEvent_050>(provider, subscribersPerEvent, subscriptions);
        publishers.P050 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_050>>();
        SubscribeCopies<ManyNotifyEvent_051>(provider, subscribersPerEvent, subscriptions);
        publishers.P051 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_051>>();
        SubscribeCopies<ManyNotifyEvent_052>(provider, subscribersPerEvent, subscriptions);
        publishers.P052 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_052>>();
        SubscribeCopies<ManyNotifyEvent_053>(provider, subscribersPerEvent, subscriptions);
        publishers.P053 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_053>>();
        SubscribeCopies<ManyNotifyEvent_054>(provider, subscribersPerEvent, subscriptions);
        publishers.P054 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_054>>();
        SubscribeCopies<ManyNotifyEvent_055>(provider, subscribersPerEvent, subscriptions);
        publishers.P055 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_055>>();
        SubscribeCopies<ManyNotifyEvent_056>(provider, subscribersPerEvent, subscriptions);
        publishers.P056 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_056>>();
        SubscribeCopies<ManyNotifyEvent_057>(provider, subscribersPerEvent, subscriptions);
        publishers.P057 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_057>>();
        SubscribeCopies<ManyNotifyEvent_058>(provider, subscribersPerEvent, subscriptions);
        publishers.P058 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_058>>();
        SubscribeCopies<ManyNotifyEvent_059>(provider, subscribersPerEvent, subscriptions);
        publishers.P059 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_059>>();
        SubscribeCopies<ManyNotifyEvent_060>(provider, subscribersPerEvent, subscriptions);
        publishers.P060 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_060>>();
        SubscribeCopies<ManyNotifyEvent_061>(provider, subscribersPerEvent, subscriptions);
        publishers.P061 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_061>>();
        SubscribeCopies<ManyNotifyEvent_062>(provider, subscribersPerEvent, subscriptions);
        publishers.P062 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_062>>();
        SubscribeCopies<ManyNotifyEvent_063>(provider, subscribersPerEvent, subscriptions);
        publishers.P063 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_063>>();
        SubscribeCopies<ManyNotifyEvent_064>(provider, subscribersPerEvent, subscriptions);
        publishers.P064 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_064>>();
        SubscribeCopies<ManyNotifyEvent_065>(provider, subscribersPerEvent, subscriptions);
        publishers.P065 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_065>>();
        SubscribeCopies<ManyNotifyEvent_066>(provider, subscribersPerEvent, subscriptions);
        publishers.P066 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_066>>();
        SubscribeCopies<ManyNotifyEvent_067>(provider, subscribersPerEvent, subscriptions);
        publishers.P067 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_067>>();
        SubscribeCopies<ManyNotifyEvent_068>(provider, subscribersPerEvent, subscriptions);
        publishers.P068 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_068>>();
        SubscribeCopies<ManyNotifyEvent_069>(provider, subscribersPerEvent, subscriptions);
        publishers.P069 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_069>>();
        SubscribeCopies<ManyNotifyEvent_070>(provider, subscribersPerEvent, subscriptions);
        publishers.P070 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_070>>();
        SubscribeCopies<ManyNotifyEvent_071>(provider, subscribersPerEvent, subscriptions);
        publishers.P071 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_071>>();
        SubscribeCopies<ManyNotifyEvent_072>(provider, subscribersPerEvent, subscriptions);
        publishers.P072 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_072>>();
        SubscribeCopies<ManyNotifyEvent_073>(provider, subscribersPerEvent, subscriptions);
        publishers.P073 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_073>>();
        SubscribeCopies<ManyNotifyEvent_074>(provider, subscribersPerEvent, subscriptions);
        publishers.P074 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_074>>();
        SubscribeCopies<ManyNotifyEvent_075>(provider, subscribersPerEvent, subscriptions);
        publishers.P075 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_075>>();
        SubscribeCopies<ManyNotifyEvent_076>(provider, subscribersPerEvent, subscriptions);
        publishers.P076 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_076>>();
        SubscribeCopies<ManyNotifyEvent_077>(provider, subscribersPerEvent, subscriptions);
        publishers.P077 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_077>>();
        SubscribeCopies<ManyNotifyEvent_078>(provider, subscribersPerEvent, subscriptions);
        publishers.P078 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_078>>();
        SubscribeCopies<ManyNotifyEvent_079>(provider, subscribersPerEvent, subscriptions);
        publishers.P079 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_079>>();
        SubscribeCopies<ManyNotifyEvent_080>(provider, subscribersPerEvent, subscriptions);
        publishers.P080 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_080>>();
        SubscribeCopies<ManyNotifyEvent_081>(provider, subscribersPerEvent, subscriptions);
        publishers.P081 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_081>>();
        SubscribeCopies<ManyNotifyEvent_082>(provider, subscribersPerEvent, subscriptions);
        publishers.P082 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_082>>();
        SubscribeCopies<ManyNotifyEvent_083>(provider, subscribersPerEvent, subscriptions);
        publishers.P083 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_083>>();
        SubscribeCopies<ManyNotifyEvent_084>(provider, subscribersPerEvent, subscriptions);
        publishers.P084 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_084>>();
        SubscribeCopies<ManyNotifyEvent_085>(provider, subscribersPerEvent, subscriptions);
        publishers.P085 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_085>>();
        SubscribeCopies<ManyNotifyEvent_086>(provider, subscribersPerEvent, subscriptions);
        publishers.P086 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_086>>();
        SubscribeCopies<ManyNotifyEvent_087>(provider, subscribersPerEvent, subscriptions);
        publishers.P087 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_087>>();
        SubscribeCopies<ManyNotifyEvent_088>(provider, subscribersPerEvent, subscriptions);
        publishers.P088 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_088>>();
        SubscribeCopies<ManyNotifyEvent_089>(provider, subscribersPerEvent, subscriptions);
        publishers.P089 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_089>>();
        SubscribeCopies<ManyNotifyEvent_090>(provider, subscribersPerEvent, subscriptions);
        publishers.P090 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_090>>();
        SubscribeCopies<ManyNotifyEvent_091>(provider, subscribersPerEvent, subscriptions);
        publishers.P091 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_091>>();
        SubscribeCopies<ManyNotifyEvent_092>(provider, subscribersPerEvent, subscriptions);
        publishers.P092 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_092>>();
        SubscribeCopies<ManyNotifyEvent_093>(provider, subscribersPerEvent, subscriptions);
        publishers.P093 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_093>>();
        SubscribeCopies<ManyNotifyEvent_094>(provider, subscribersPerEvent, subscriptions);
        publishers.P094 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_094>>();
        SubscribeCopies<ManyNotifyEvent_095>(provider, subscribersPerEvent, subscriptions);
        publishers.P095 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_095>>();
        SubscribeCopies<ManyNotifyEvent_096>(provider, subscribersPerEvent, subscriptions);
        publishers.P096 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_096>>();
        SubscribeCopies<ManyNotifyEvent_097>(provider, subscribersPerEvent, subscriptions);
        publishers.P097 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_097>>();
        SubscribeCopies<ManyNotifyEvent_098>(provider, subscribersPerEvent, subscriptions);
        publishers.P098 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_098>>();
        SubscribeCopies<ManyNotifyEvent_099>(provider, subscribersPerEvent, subscriptions);
        publishers.P099 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_099>>();
        SubscribeCopies<ManyNotifyEvent_100>(provider, subscribersPerEvent, subscriptions);
        publishers.P100 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_100>>();
        SubscribeCopies<ManyNotifyEvent_101>(provider, subscribersPerEvent, subscriptions);
        publishers.P101 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_101>>();
        SubscribeCopies<ManyNotifyEvent_102>(provider, subscribersPerEvent, subscriptions);
        publishers.P102 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_102>>();
        SubscribeCopies<ManyNotifyEvent_103>(provider, subscribersPerEvent, subscriptions);
        publishers.P103 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_103>>();
        SubscribeCopies<ManyNotifyEvent_104>(provider, subscribersPerEvent, subscriptions);
        publishers.P104 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_104>>();
        SubscribeCopies<ManyNotifyEvent_105>(provider, subscribersPerEvent, subscriptions);
        publishers.P105 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_105>>();
        SubscribeCopies<ManyNotifyEvent_106>(provider, subscribersPerEvent, subscriptions);
        publishers.P106 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_106>>();
        SubscribeCopies<ManyNotifyEvent_107>(provider, subscribersPerEvent, subscriptions);
        publishers.P107 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_107>>();
        SubscribeCopies<ManyNotifyEvent_108>(provider, subscribersPerEvent, subscriptions);
        publishers.P108 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_108>>();
        SubscribeCopies<ManyNotifyEvent_109>(provider, subscribersPerEvent, subscriptions);
        publishers.P109 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_109>>();
        SubscribeCopies<ManyNotifyEvent_110>(provider, subscribersPerEvent, subscriptions);
        publishers.P110 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_110>>();
        SubscribeCopies<ManyNotifyEvent_111>(provider, subscribersPerEvent, subscriptions);
        publishers.P111 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_111>>();
        SubscribeCopies<ManyNotifyEvent_112>(provider, subscribersPerEvent, subscriptions);
        publishers.P112 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_112>>();
        SubscribeCopies<ManyNotifyEvent_113>(provider, subscribersPerEvent, subscriptions);
        publishers.P113 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_113>>();
        SubscribeCopies<ManyNotifyEvent_114>(provider, subscribersPerEvent, subscriptions);
        publishers.P114 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_114>>();
        SubscribeCopies<ManyNotifyEvent_115>(provider, subscribersPerEvent, subscriptions);
        publishers.P115 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_115>>();
        SubscribeCopies<ManyNotifyEvent_116>(provider, subscribersPerEvent, subscriptions);
        publishers.P116 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_116>>();
        SubscribeCopies<ManyNotifyEvent_117>(provider, subscribersPerEvent, subscriptions);
        publishers.P117 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_117>>();
        SubscribeCopies<ManyNotifyEvent_118>(provider, subscribersPerEvent, subscriptions);
        publishers.P118 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_118>>();
        SubscribeCopies<ManyNotifyEvent_119>(provider, subscribersPerEvent, subscriptions);
        publishers.P119 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_119>>();
        SubscribeCopies<ManyNotifyEvent_120>(provider, subscribersPerEvent, subscriptions);
        publishers.P120 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_120>>();
        SubscribeCopies<ManyNotifyEvent_121>(provider, subscribersPerEvent, subscriptions);
        publishers.P121 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_121>>();
        SubscribeCopies<ManyNotifyEvent_122>(provider, subscribersPerEvent, subscriptions);
        publishers.P122 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_122>>();
        SubscribeCopies<ManyNotifyEvent_123>(provider, subscribersPerEvent, subscriptions);
        publishers.P123 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_123>>();
        SubscribeCopies<ManyNotifyEvent_124>(provider, subscribersPerEvent, subscriptions);
        publishers.P124 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_124>>();
        SubscribeCopies<ManyNotifyEvent_125>(provider, subscribersPerEvent, subscriptions);
        publishers.P125 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_125>>();
        SubscribeCopies<ManyNotifyEvent_126>(provider, subscribersPerEvent, subscriptions);
        publishers.P126 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_126>>();
        SubscribeCopies<ManyNotifyEvent_127>(provider, subscribersPerEvent, subscriptions);
        publishers.P127 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_127>>();
        SubscribeCopies<ManyNotifyEvent_128>(provider, subscribersPerEvent, subscriptions);
        publishers.P128 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_128>>();
        SubscribeCopies<ManyNotifyEvent_129>(provider, subscribersPerEvent, subscriptions);
        publishers.P129 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_129>>();
        SubscribeCopies<ManyNotifyEvent_130>(provider, subscribersPerEvent, subscriptions);
        publishers.P130 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_130>>();
        SubscribeCopies<ManyNotifyEvent_131>(provider, subscribersPerEvent, subscriptions);
        publishers.P131 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_131>>();
        SubscribeCopies<ManyNotifyEvent_132>(provider, subscribersPerEvent, subscriptions);
        publishers.P132 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_132>>();
        SubscribeCopies<ManyNotifyEvent_133>(provider, subscribersPerEvent, subscriptions);
        publishers.P133 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_133>>();
        SubscribeCopies<ManyNotifyEvent_134>(provider, subscribersPerEvent, subscriptions);
        publishers.P134 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_134>>();
        SubscribeCopies<ManyNotifyEvent_135>(provider, subscribersPerEvent, subscriptions);
        publishers.P135 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_135>>();
        SubscribeCopies<ManyNotifyEvent_136>(provider, subscribersPerEvent, subscriptions);
        publishers.P136 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_136>>();
        SubscribeCopies<ManyNotifyEvent_137>(provider, subscribersPerEvent, subscriptions);
        publishers.P137 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_137>>();
        SubscribeCopies<ManyNotifyEvent_138>(provider, subscribersPerEvent, subscriptions);
        publishers.P138 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_138>>();
        SubscribeCopies<ManyNotifyEvent_139>(provider, subscribersPerEvent, subscriptions);
        publishers.P139 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_139>>();
        SubscribeCopies<ManyNotifyEvent_140>(provider, subscribersPerEvent, subscriptions);
        publishers.P140 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_140>>();
        SubscribeCopies<ManyNotifyEvent_141>(provider, subscribersPerEvent, subscriptions);
        publishers.P141 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_141>>();
        SubscribeCopies<ManyNotifyEvent_142>(provider, subscribersPerEvent, subscriptions);
        publishers.P142 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_142>>();
        SubscribeCopies<ManyNotifyEvent_143>(provider, subscribersPerEvent, subscriptions);
        publishers.P143 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_143>>();
        SubscribeCopies<ManyNotifyEvent_144>(provider, subscribersPerEvent, subscriptions);
        publishers.P144 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_144>>();
        SubscribeCopies<ManyNotifyEvent_145>(provider, subscribersPerEvent, subscriptions);
        publishers.P145 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_145>>();
        SubscribeCopies<ManyNotifyEvent_146>(provider, subscribersPerEvent, subscriptions);
        publishers.P146 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_146>>();
        SubscribeCopies<ManyNotifyEvent_147>(provider, subscribersPerEvent, subscriptions);
        publishers.P147 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_147>>();
        SubscribeCopies<ManyNotifyEvent_148>(provider, subscribersPerEvent, subscriptions);
        publishers.P148 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_148>>();
        SubscribeCopies<ManyNotifyEvent_149>(provider, subscribersPerEvent, subscriptions);
        publishers.P149 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_149>>();
        SubscribeCopies<ManyNotifyEvent_150>(provider, subscribersPerEvent, subscriptions);
        publishers.P150 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_150>>();
        SubscribeCopies<ManyNotifyEvent_151>(provider, subscribersPerEvent, subscriptions);
        publishers.P151 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_151>>();
        SubscribeCopies<ManyNotifyEvent_152>(provider, subscribersPerEvent, subscriptions);
        publishers.P152 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_152>>();
        SubscribeCopies<ManyNotifyEvent_153>(provider, subscribersPerEvent, subscriptions);
        publishers.P153 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_153>>();
        SubscribeCopies<ManyNotifyEvent_154>(provider, subscribersPerEvent, subscriptions);
        publishers.P154 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_154>>();
        SubscribeCopies<ManyNotifyEvent_155>(provider, subscribersPerEvent, subscriptions);
        publishers.P155 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_155>>();
        SubscribeCopies<ManyNotifyEvent_156>(provider, subscribersPerEvent, subscriptions);
        publishers.P156 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_156>>();
        SubscribeCopies<ManyNotifyEvent_157>(provider, subscribersPerEvent, subscriptions);
        publishers.P157 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_157>>();
        SubscribeCopies<ManyNotifyEvent_158>(provider, subscribersPerEvent, subscriptions);
        publishers.P158 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_158>>();
        SubscribeCopies<ManyNotifyEvent_159>(provider, subscribersPerEvent, subscriptions);
        publishers.P159 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_159>>();
        SubscribeCopies<ManyNotifyEvent_160>(provider, subscribersPerEvent, subscriptions);
        publishers.P160 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_160>>();
        SubscribeCopies<ManyNotifyEvent_161>(provider, subscribersPerEvent, subscriptions);
        publishers.P161 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_161>>();
        SubscribeCopies<ManyNotifyEvent_162>(provider, subscribersPerEvent, subscriptions);
        publishers.P162 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_162>>();
        SubscribeCopies<ManyNotifyEvent_163>(provider, subscribersPerEvent, subscriptions);
        publishers.P163 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_163>>();
        SubscribeCopies<ManyNotifyEvent_164>(provider, subscribersPerEvent, subscriptions);
        publishers.P164 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_164>>();
        SubscribeCopies<ManyNotifyEvent_165>(provider, subscribersPerEvent, subscriptions);
        publishers.P165 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_165>>();
        SubscribeCopies<ManyNotifyEvent_166>(provider, subscribersPerEvent, subscriptions);
        publishers.P166 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_166>>();
        SubscribeCopies<ManyNotifyEvent_167>(provider, subscribersPerEvent, subscriptions);
        publishers.P167 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_167>>();
        SubscribeCopies<ManyNotifyEvent_168>(provider, subscribersPerEvent, subscriptions);
        publishers.P168 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_168>>();
        SubscribeCopies<ManyNotifyEvent_169>(provider, subscribersPerEvent, subscriptions);
        publishers.P169 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_169>>();
        SubscribeCopies<ManyNotifyEvent_170>(provider, subscribersPerEvent, subscriptions);
        publishers.P170 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_170>>();
        SubscribeCopies<ManyNotifyEvent_171>(provider, subscribersPerEvent, subscriptions);
        publishers.P171 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_171>>();
        SubscribeCopies<ManyNotifyEvent_172>(provider, subscribersPerEvent, subscriptions);
        publishers.P172 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_172>>();
        SubscribeCopies<ManyNotifyEvent_173>(provider, subscribersPerEvent, subscriptions);
        publishers.P173 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_173>>();
        SubscribeCopies<ManyNotifyEvent_174>(provider, subscribersPerEvent, subscriptions);
        publishers.P174 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_174>>();
        SubscribeCopies<ManyNotifyEvent_175>(provider, subscribersPerEvent, subscriptions);
        publishers.P175 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_175>>();
        SubscribeCopies<ManyNotifyEvent_176>(provider, subscribersPerEvent, subscriptions);
        publishers.P176 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_176>>();
        SubscribeCopies<ManyNotifyEvent_177>(provider, subscribersPerEvent, subscriptions);
        publishers.P177 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_177>>();
        SubscribeCopies<ManyNotifyEvent_178>(provider, subscribersPerEvent, subscriptions);
        publishers.P178 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_178>>();
        SubscribeCopies<ManyNotifyEvent_179>(provider, subscribersPerEvent, subscriptions);
        publishers.P179 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_179>>();
        SubscribeCopies<ManyNotifyEvent_180>(provider, subscribersPerEvent, subscriptions);
        publishers.P180 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_180>>();
        SubscribeCopies<ManyNotifyEvent_181>(provider, subscribersPerEvent, subscriptions);
        publishers.P181 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_181>>();
        SubscribeCopies<ManyNotifyEvent_182>(provider, subscribersPerEvent, subscriptions);
        publishers.P182 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_182>>();
        SubscribeCopies<ManyNotifyEvent_183>(provider, subscribersPerEvent, subscriptions);
        publishers.P183 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_183>>();
        SubscribeCopies<ManyNotifyEvent_184>(provider, subscribersPerEvent, subscriptions);
        publishers.P184 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_184>>();
        SubscribeCopies<ManyNotifyEvent_185>(provider, subscribersPerEvent, subscriptions);
        publishers.P185 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_185>>();
        SubscribeCopies<ManyNotifyEvent_186>(provider, subscribersPerEvent, subscriptions);
        publishers.P186 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_186>>();
        SubscribeCopies<ManyNotifyEvent_187>(provider, subscribersPerEvent, subscriptions);
        publishers.P187 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_187>>();
        SubscribeCopies<ManyNotifyEvent_188>(provider, subscribersPerEvent, subscriptions);
        publishers.P188 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_188>>();
        SubscribeCopies<ManyNotifyEvent_189>(provider, subscribersPerEvent, subscriptions);
        publishers.P189 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_189>>();
        SubscribeCopies<ManyNotifyEvent_190>(provider, subscribersPerEvent, subscriptions);
        publishers.P190 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_190>>();
        SubscribeCopies<ManyNotifyEvent_191>(provider, subscribersPerEvent, subscriptions);
        publishers.P191 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_191>>();
        SubscribeCopies<ManyNotifyEvent_192>(provider, subscribersPerEvent, subscriptions);
        publishers.P192 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_192>>();
        SubscribeCopies<ManyNotifyEvent_193>(provider, subscribersPerEvent, subscriptions);
        publishers.P193 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_193>>();
        SubscribeCopies<ManyNotifyEvent_194>(provider, subscribersPerEvent, subscriptions);
        publishers.P194 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_194>>();
        SubscribeCopies<ManyNotifyEvent_195>(provider, subscribersPerEvent, subscriptions);
        publishers.P195 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_195>>();
        SubscribeCopies<ManyNotifyEvent_196>(provider, subscribersPerEvent, subscriptions);
        publishers.P196 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_196>>();
        SubscribeCopies<ManyNotifyEvent_197>(provider, subscribersPerEvent, subscriptions);
        publishers.P197 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_197>>();
        SubscribeCopies<ManyNotifyEvent_198>(provider, subscribersPerEvent, subscriptions);
        publishers.P198 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_198>>();
        SubscribeCopies<ManyNotifyEvent_199>(provider, subscribersPerEvent, subscriptions);
        publishers.P199 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_199>>();
        SubscribeCopies<ManyNotifyEvent_200>(provider, subscribersPerEvent, subscriptions);
        publishers.P200 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_200>>();
        SubscribeCopies<ManyNotifyEvent_201>(provider, subscribersPerEvent, subscriptions);
        publishers.P201 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_201>>();
        SubscribeCopies<ManyNotifyEvent_202>(provider, subscribersPerEvent, subscriptions);
        publishers.P202 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_202>>();
        SubscribeCopies<ManyNotifyEvent_203>(provider, subscribersPerEvent, subscriptions);
        publishers.P203 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_203>>();
        SubscribeCopies<ManyNotifyEvent_204>(provider, subscribersPerEvent, subscriptions);
        publishers.P204 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_204>>();
        SubscribeCopies<ManyNotifyEvent_205>(provider, subscribersPerEvent, subscriptions);
        publishers.P205 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_205>>();
        SubscribeCopies<ManyNotifyEvent_206>(provider, subscribersPerEvent, subscriptions);
        publishers.P206 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_206>>();
        SubscribeCopies<ManyNotifyEvent_207>(provider, subscribersPerEvent, subscriptions);
        publishers.P207 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_207>>();
        SubscribeCopies<ManyNotifyEvent_208>(provider, subscribersPerEvent, subscriptions);
        publishers.P208 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_208>>();
        SubscribeCopies<ManyNotifyEvent_209>(provider, subscribersPerEvent, subscriptions);
        publishers.P209 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_209>>();
        SubscribeCopies<ManyNotifyEvent_210>(provider, subscribersPerEvent, subscriptions);
        publishers.P210 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_210>>();
        SubscribeCopies<ManyNotifyEvent_211>(provider, subscribersPerEvent, subscriptions);
        publishers.P211 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_211>>();
        SubscribeCopies<ManyNotifyEvent_212>(provider, subscribersPerEvent, subscriptions);
        publishers.P212 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_212>>();
        SubscribeCopies<ManyNotifyEvent_213>(provider, subscribersPerEvent, subscriptions);
        publishers.P213 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_213>>();
        SubscribeCopies<ManyNotifyEvent_214>(provider, subscribersPerEvent, subscriptions);
        publishers.P214 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_214>>();
        SubscribeCopies<ManyNotifyEvent_215>(provider, subscribersPerEvent, subscriptions);
        publishers.P215 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_215>>();
        SubscribeCopies<ManyNotifyEvent_216>(provider, subscribersPerEvent, subscriptions);
        publishers.P216 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_216>>();
        SubscribeCopies<ManyNotifyEvent_217>(provider, subscribersPerEvent, subscriptions);
        publishers.P217 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_217>>();
        SubscribeCopies<ManyNotifyEvent_218>(provider, subscribersPerEvent, subscriptions);
        publishers.P218 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_218>>();
        SubscribeCopies<ManyNotifyEvent_219>(provider, subscribersPerEvent, subscriptions);
        publishers.P219 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_219>>();
        SubscribeCopies<ManyNotifyEvent_220>(provider, subscribersPerEvent, subscriptions);
        publishers.P220 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_220>>();
        SubscribeCopies<ManyNotifyEvent_221>(provider, subscribersPerEvent, subscriptions);
        publishers.P221 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_221>>();
        SubscribeCopies<ManyNotifyEvent_222>(provider, subscribersPerEvent, subscriptions);
        publishers.P222 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_222>>();
        SubscribeCopies<ManyNotifyEvent_223>(provider, subscribersPerEvent, subscriptions);
        publishers.P223 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_223>>();
        SubscribeCopies<ManyNotifyEvent_224>(provider, subscribersPerEvent, subscriptions);
        publishers.P224 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_224>>();
        SubscribeCopies<ManyNotifyEvent_225>(provider, subscribersPerEvent, subscriptions);
        publishers.P225 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_225>>();
        SubscribeCopies<ManyNotifyEvent_226>(provider, subscribersPerEvent, subscriptions);
        publishers.P226 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_226>>();
        SubscribeCopies<ManyNotifyEvent_227>(provider, subscribersPerEvent, subscriptions);
        publishers.P227 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_227>>();
        SubscribeCopies<ManyNotifyEvent_228>(provider, subscribersPerEvent, subscriptions);
        publishers.P228 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_228>>();
        SubscribeCopies<ManyNotifyEvent_229>(provider, subscribersPerEvent, subscriptions);
        publishers.P229 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_229>>();
        SubscribeCopies<ManyNotifyEvent_230>(provider, subscribersPerEvent, subscriptions);
        publishers.P230 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_230>>();
        SubscribeCopies<ManyNotifyEvent_231>(provider, subscribersPerEvent, subscriptions);
        publishers.P231 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_231>>();
        SubscribeCopies<ManyNotifyEvent_232>(provider, subscribersPerEvent, subscriptions);
        publishers.P232 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_232>>();
        SubscribeCopies<ManyNotifyEvent_233>(provider, subscribersPerEvent, subscriptions);
        publishers.P233 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_233>>();
        SubscribeCopies<ManyNotifyEvent_234>(provider, subscribersPerEvent, subscriptions);
        publishers.P234 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_234>>();
        SubscribeCopies<ManyNotifyEvent_235>(provider, subscribersPerEvent, subscriptions);
        publishers.P235 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_235>>();
        SubscribeCopies<ManyNotifyEvent_236>(provider, subscribersPerEvent, subscriptions);
        publishers.P236 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_236>>();
        SubscribeCopies<ManyNotifyEvent_237>(provider, subscribersPerEvent, subscriptions);
        publishers.P237 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_237>>();
        SubscribeCopies<ManyNotifyEvent_238>(provider, subscribersPerEvent, subscriptions);
        publishers.P238 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_238>>();
        SubscribeCopies<ManyNotifyEvent_239>(provider, subscribersPerEvent, subscriptions);
        publishers.P239 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_239>>();
        SubscribeCopies<ManyNotifyEvent_240>(provider, subscribersPerEvent, subscriptions);
        publishers.P240 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_240>>();
        SubscribeCopies<ManyNotifyEvent_241>(provider, subscribersPerEvent, subscriptions);
        publishers.P241 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_241>>();
        SubscribeCopies<ManyNotifyEvent_242>(provider, subscribersPerEvent, subscriptions);
        publishers.P242 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_242>>();
        SubscribeCopies<ManyNotifyEvent_243>(provider, subscribersPerEvent, subscriptions);
        publishers.P243 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_243>>();
        SubscribeCopies<ManyNotifyEvent_244>(provider, subscribersPerEvent, subscriptions);
        publishers.P244 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_244>>();
        SubscribeCopies<ManyNotifyEvent_245>(provider, subscribersPerEvent, subscriptions);
        publishers.P245 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_245>>();
        SubscribeCopies<ManyNotifyEvent_246>(provider, subscribersPerEvent, subscriptions);
        publishers.P246 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_246>>();
        SubscribeCopies<ManyNotifyEvent_247>(provider, subscribersPerEvent, subscriptions);
        publishers.P247 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_247>>();
        SubscribeCopies<ManyNotifyEvent_248>(provider, subscribersPerEvent, subscriptions);
        publishers.P248 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_248>>();
        SubscribeCopies<ManyNotifyEvent_249>(provider, subscribersPerEvent, subscriptions);
        publishers.P249 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_249>>();
        SubscribeCopies<ManyNotifyEvent_250>(provider, subscribersPerEvent, subscriptions);
        publishers.P250 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_250>>();
        SubscribeCopies<ManyNotifyEvent_251>(provider, subscribersPerEvent, subscriptions);
        publishers.P251 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_251>>();
        SubscribeCopies<ManyNotifyEvent_252>(provider, subscribersPerEvent, subscriptions);
        publishers.P252 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_252>>();
        SubscribeCopies<ManyNotifyEvent_253>(provider, subscribersPerEvent, subscriptions);
        publishers.P253 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_253>>();
        SubscribeCopies<ManyNotifyEvent_254>(provider, subscribersPerEvent, subscriptions);
        publishers.P254 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_254>>();
        SubscribeCopies<ManyNotifyEvent_255>(provider, subscribersPerEvent, subscriptions);
        publishers.P255 = provider.GetRequiredService<IPublisher<ManyNotifyEvent_255>>();
        return publishers;
    }

    public static void DispatchDirect256(int subscribersPerEvent)
    {
        if (subscribersPerEvent == 2)
        {
            DirectConsume(in ManyNotifyEvent_000.Instance);
            DirectConsume(in ManyNotifyEvent_000.Instance);
            DirectConsume(in ManyNotifyEvent_001.Instance);
            DirectConsume(in ManyNotifyEvent_001.Instance);
            DirectConsume(in ManyNotifyEvent_002.Instance);
            DirectConsume(in ManyNotifyEvent_002.Instance);
            DirectConsume(in ManyNotifyEvent_003.Instance);
            DirectConsume(in ManyNotifyEvent_003.Instance);
            DirectConsume(in ManyNotifyEvent_004.Instance);
            DirectConsume(in ManyNotifyEvent_004.Instance);
            DirectConsume(in ManyNotifyEvent_005.Instance);
            DirectConsume(in ManyNotifyEvent_005.Instance);
            DirectConsume(in ManyNotifyEvent_006.Instance);
            DirectConsume(in ManyNotifyEvent_006.Instance);
            DirectConsume(in ManyNotifyEvent_007.Instance);
            DirectConsume(in ManyNotifyEvent_007.Instance);
            DirectConsume(in ManyNotifyEvent_008.Instance);
            DirectConsume(in ManyNotifyEvent_008.Instance);
            DirectConsume(in ManyNotifyEvent_009.Instance);
            DirectConsume(in ManyNotifyEvent_009.Instance);
            DirectConsume(in ManyNotifyEvent_010.Instance);
            DirectConsume(in ManyNotifyEvent_010.Instance);
            DirectConsume(in ManyNotifyEvent_011.Instance);
            DirectConsume(in ManyNotifyEvent_011.Instance);
            DirectConsume(in ManyNotifyEvent_012.Instance);
            DirectConsume(in ManyNotifyEvent_012.Instance);
            DirectConsume(in ManyNotifyEvent_013.Instance);
            DirectConsume(in ManyNotifyEvent_013.Instance);
            DirectConsume(in ManyNotifyEvent_014.Instance);
            DirectConsume(in ManyNotifyEvent_014.Instance);
            DirectConsume(in ManyNotifyEvent_015.Instance);
            DirectConsume(in ManyNotifyEvent_015.Instance);
            DirectConsume(in ManyNotifyEvent_016.Instance);
            DirectConsume(in ManyNotifyEvent_016.Instance);
            DirectConsume(in ManyNotifyEvent_017.Instance);
            DirectConsume(in ManyNotifyEvent_017.Instance);
            DirectConsume(in ManyNotifyEvent_018.Instance);
            DirectConsume(in ManyNotifyEvent_018.Instance);
            DirectConsume(in ManyNotifyEvent_019.Instance);
            DirectConsume(in ManyNotifyEvent_019.Instance);
            DirectConsume(in ManyNotifyEvent_020.Instance);
            DirectConsume(in ManyNotifyEvent_020.Instance);
            DirectConsume(in ManyNotifyEvent_021.Instance);
            DirectConsume(in ManyNotifyEvent_021.Instance);
            DirectConsume(in ManyNotifyEvent_022.Instance);
            DirectConsume(in ManyNotifyEvent_022.Instance);
            DirectConsume(in ManyNotifyEvent_023.Instance);
            DirectConsume(in ManyNotifyEvent_023.Instance);
            DirectConsume(in ManyNotifyEvent_024.Instance);
            DirectConsume(in ManyNotifyEvent_024.Instance);
            DirectConsume(in ManyNotifyEvent_025.Instance);
            DirectConsume(in ManyNotifyEvent_025.Instance);
            DirectConsume(in ManyNotifyEvent_026.Instance);
            DirectConsume(in ManyNotifyEvent_026.Instance);
            DirectConsume(in ManyNotifyEvent_027.Instance);
            DirectConsume(in ManyNotifyEvent_027.Instance);
            DirectConsume(in ManyNotifyEvent_028.Instance);
            DirectConsume(in ManyNotifyEvent_028.Instance);
            DirectConsume(in ManyNotifyEvent_029.Instance);
            DirectConsume(in ManyNotifyEvent_029.Instance);
            DirectConsume(in ManyNotifyEvent_030.Instance);
            DirectConsume(in ManyNotifyEvent_030.Instance);
            DirectConsume(in ManyNotifyEvent_031.Instance);
            DirectConsume(in ManyNotifyEvent_031.Instance);
            DirectConsume(in ManyNotifyEvent_032.Instance);
            DirectConsume(in ManyNotifyEvent_032.Instance);
            DirectConsume(in ManyNotifyEvent_033.Instance);
            DirectConsume(in ManyNotifyEvent_033.Instance);
            DirectConsume(in ManyNotifyEvent_034.Instance);
            DirectConsume(in ManyNotifyEvent_034.Instance);
            DirectConsume(in ManyNotifyEvent_035.Instance);
            DirectConsume(in ManyNotifyEvent_035.Instance);
            DirectConsume(in ManyNotifyEvent_036.Instance);
            DirectConsume(in ManyNotifyEvent_036.Instance);
            DirectConsume(in ManyNotifyEvent_037.Instance);
            DirectConsume(in ManyNotifyEvent_037.Instance);
            DirectConsume(in ManyNotifyEvent_038.Instance);
            DirectConsume(in ManyNotifyEvent_038.Instance);
            DirectConsume(in ManyNotifyEvent_039.Instance);
            DirectConsume(in ManyNotifyEvent_039.Instance);
            DirectConsume(in ManyNotifyEvent_040.Instance);
            DirectConsume(in ManyNotifyEvent_040.Instance);
            DirectConsume(in ManyNotifyEvent_041.Instance);
            DirectConsume(in ManyNotifyEvent_041.Instance);
            DirectConsume(in ManyNotifyEvent_042.Instance);
            DirectConsume(in ManyNotifyEvent_042.Instance);
            DirectConsume(in ManyNotifyEvent_043.Instance);
            DirectConsume(in ManyNotifyEvent_043.Instance);
            DirectConsume(in ManyNotifyEvent_044.Instance);
            DirectConsume(in ManyNotifyEvent_044.Instance);
            DirectConsume(in ManyNotifyEvent_045.Instance);
            DirectConsume(in ManyNotifyEvent_045.Instance);
            DirectConsume(in ManyNotifyEvent_046.Instance);
            DirectConsume(in ManyNotifyEvent_046.Instance);
            DirectConsume(in ManyNotifyEvent_047.Instance);
            DirectConsume(in ManyNotifyEvent_047.Instance);
            DirectConsume(in ManyNotifyEvent_048.Instance);
            DirectConsume(in ManyNotifyEvent_048.Instance);
            DirectConsume(in ManyNotifyEvent_049.Instance);
            DirectConsume(in ManyNotifyEvent_049.Instance);
            DirectConsume(in ManyNotifyEvent_050.Instance);
            DirectConsume(in ManyNotifyEvent_050.Instance);
            DirectConsume(in ManyNotifyEvent_051.Instance);
            DirectConsume(in ManyNotifyEvent_051.Instance);
            DirectConsume(in ManyNotifyEvent_052.Instance);
            DirectConsume(in ManyNotifyEvent_052.Instance);
            DirectConsume(in ManyNotifyEvent_053.Instance);
            DirectConsume(in ManyNotifyEvent_053.Instance);
            DirectConsume(in ManyNotifyEvent_054.Instance);
            DirectConsume(in ManyNotifyEvent_054.Instance);
            DirectConsume(in ManyNotifyEvent_055.Instance);
            DirectConsume(in ManyNotifyEvent_055.Instance);
            DirectConsume(in ManyNotifyEvent_056.Instance);
            DirectConsume(in ManyNotifyEvent_056.Instance);
            DirectConsume(in ManyNotifyEvent_057.Instance);
            DirectConsume(in ManyNotifyEvent_057.Instance);
            DirectConsume(in ManyNotifyEvent_058.Instance);
            DirectConsume(in ManyNotifyEvent_058.Instance);
            DirectConsume(in ManyNotifyEvent_059.Instance);
            DirectConsume(in ManyNotifyEvent_059.Instance);
            DirectConsume(in ManyNotifyEvent_060.Instance);
            DirectConsume(in ManyNotifyEvent_060.Instance);
            DirectConsume(in ManyNotifyEvent_061.Instance);
            DirectConsume(in ManyNotifyEvent_061.Instance);
            DirectConsume(in ManyNotifyEvent_062.Instance);
            DirectConsume(in ManyNotifyEvent_062.Instance);
            DirectConsume(in ManyNotifyEvent_063.Instance);
            DirectConsume(in ManyNotifyEvent_063.Instance);
            DirectConsume(in ManyNotifyEvent_064.Instance);
            DirectConsume(in ManyNotifyEvent_064.Instance);
            DirectConsume(in ManyNotifyEvent_065.Instance);
            DirectConsume(in ManyNotifyEvent_065.Instance);
            DirectConsume(in ManyNotifyEvent_066.Instance);
            DirectConsume(in ManyNotifyEvent_066.Instance);
            DirectConsume(in ManyNotifyEvent_067.Instance);
            DirectConsume(in ManyNotifyEvent_067.Instance);
            DirectConsume(in ManyNotifyEvent_068.Instance);
            DirectConsume(in ManyNotifyEvent_068.Instance);
            DirectConsume(in ManyNotifyEvent_069.Instance);
            DirectConsume(in ManyNotifyEvent_069.Instance);
            DirectConsume(in ManyNotifyEvent_070.Instance);
            DirectConsume(in ManyNotifyEvent_070.Instance);
            DirectConsume(in ManyNotifyEvent_071.Instance);
            DirectConsume(in ManyNotifyEvent_071.Instance);
            DirectConsume(in ManyNotifyEvent_072.Instance);
            DirectConsume(in ManyNotifyEvent_072.Instance);
            DirectConsume(in ManyNotifyEvent_073.Instance);
            DirectConsume(in ManyNotifyEvent_073.Instance);
            DirectConsume(in ManyNotifyEvent_074.Instance);
            DirectConsume(in ManyNotifyEvent_074.Instance);
            DirectConsume(in ManyNotifyEvent_075.Instance);
            DirectConsume(in ManyNotifyEvent_075.Instance);
            DirectConsume(in ManyNotifyEvent_076.Instance);
            DirectConsume(in ManyNotifyEvent_076.Instance);
            DirectConsume(in ManyNotifyEvent_077.Instance);
            DirectConsume(in ManyNotifyEvent_077.Instance);
            DirectConsume(in ManyNotifyEvent_078.Instance);
            DirectConsume(in ManyNotifyEvent_078.Instance);
            DirectConsume(in ManyNotifyEvent_079.Instance);
            DirectConsume(in ManyNotifyEvent_079.Instance);
            DirectConsume(in ManyNotifyEvent_080.Instance);
            DirectConsume(in ManyNotifyEvent_080.Instance);
            DirectConsume(in ManyNotifyEvent_081.Instance);
            DirectConsume(in ManyNotifyEvent_081.Instance);
            DirectConsume(in ManyNotifyEvent_082.Instance);
            DirectConsume(in ManyNotifyEvent_082.Instance);
            DirectConsume(in ManyNotifyEvent_083.Instance);
            DirectConsume(in ManyNotifyEvent_083.Instance);
            DirectConsume(in ManyNotifyEvent_084.Instance);
            DirectConsume(in ManyNotifyEvent_084.Instance);
            DirectConsume(in ManyNotifyEvent_085.Instance);
            DirectConsume(in ManyNotifyEvent_085.Instance);
            DirectConsume(in ManyNotifyEvent_086.Instance);
            DirectConsume(in ManyNotifyEvent_086.Instance);
            DirectConsume(in ManyNotifyEvent_087.Instance);
            DirectConsume(in ManyNotifyEvent_087.Instance);
            DirectConsume(in ManyNotifyEvent_088.Instance);
            DirectConsume(in ManyNotifyEvent_088.Instance);
            DirectConsume(in ManyNotifyEvent_089.Instance);
            DirectConsume(in ManyNotifyEvent_089.Instance);
            DirectConsume(in ManyNotifyEvent_090.Instance);
            DirectConsume(in ManyNotifyEvent_090.Instance);
            DirectConsume(in ManyNotifyEvent_091.Instance);
            DirectConsume(in ManyNotifyEvent_091.Instance);
            DirectConsume(in ManyNotifyEvent_092.Instance);
            DirectConsume(in ManyNotifyEvent_092.Instance);
            DirectConsume(in ManyNotifyEvent_093.Instance);
            DirectConsume(in ManyNotifyEvent_093.Instance);
            DirectConsume(in ManyNotifyEvent_094.Instance);
            DirectConsume(in ManyNotifyEvent_094.Instance);
            DirectConsume(in ManyNotifyEvent_095.Instance);
            DirectConsume(in ManyNotifyEvent_095.Instance);
            DirectConsume(in ManyNotifyEvent_096.Instance);
            DirectConsume(in ManyNotifyEvent_096.Instance);
            DirectConsume(in ManyNotifyEvent_097.Instance);
            DirectConsume(in ManyNotifyEvent_097.Instance);
            DirectConsume(in ManyNotifyEvent_098.Instance);
            DirectConsume(in ManyNotifyEvent_098.Instance);
            DirectConsume(in ManyNotifyEvent_099.Instance);
            DirectConsume(in ManyNotifyEvent_099.Instance);
            DirectConsume(in ManyNotifyEvent_100.Instance);
            DirectConsume(in ManyNotifyEvent_100.Instance);
            DirectConsume(in ManyNotifyEvent_101.Instance);
            DirectConsume(in ManyNotifyEvent_101.Instance);
            DirectConsume(in ManyNotifyEvent_102.Instance);
            DirectConsume(in ManyNotifyEvent_102.Instance);
            DirectConsume(in ManyNotifyEvent_103.Instance);
            DirectConsume(in ManyNotifyEvent_103.Instance);
            DirectConsume(in ManyNotifyEvent_104.Instance);
            DirectConsume(in ManyNotifyEvent_104.Instance);
            DirectConsume(in ManyNotifyEvent_105.Instance);
            DirectConsume(in ManyNotifyEvent_105.Instance);
            DirectConsume(in ManyNotifyEvent_106.Instance);
            DirectConsume(in ManyNotifyEvent_106.Instance);
            DirectConsume(in ManyNotifyEvent_107.Instance);
            DirectConsume(in ManyNotifyEvent_107.Instance);
            DirectConsume(in ManyNotifyEvent_108.Instance);
            DirectConsume(in ManyNotifyEvent_108.Instance);
            DirectConsume(in ManyNotifyEvent_109.Instance);
            DirectConsume(in ManyNotifyEvent_109.Instance);
            DirectConsume(in ManyNotifyEvent_110.Instance);
            DirectConsume(in ManyNotifyEvent_110.Instance);
            DirectConsume(in ManyNotifyEvent_111.Instance);
            DirectConsume(in ManyNotifyEvent_111.Instance);
            DirectConsume(in ManyNotifyEvent_112.Instance);
            DirectConsume(in ManyNotifyEvent_112.Instance);
            DirectConsume(in ManyNotifyEvent_113.Instance);
            DirectConsume(in ManyNotifyEvent_113.Instance);
            DirectConsume(in ManyNotifyEvent_114.Instance);
            DirectConsume(in ManyNotifyEvent_114.Instance);
            DirectConsume(in ManyNotifyEvent_115.Instance);
            DirectConsume(in ManyNotifyEvent_115.Instance);
            DirectConsume(in ManyNotifyEvent_116.Instance);
            DirectConsume(in ManyNotifyEvent_116.Instance);
            DirectConsume(in ManyNotifyEvent_117.Instance);
            DirectConsume(in ManyNotifyEvent_117.Instance);
            DirectConsume(in ManyNotifyEvent_118.Instance);
            DirectConsume(in ManyNotifyEvent_118.Instance);
            DirectConsume(in ManyNotifyEvent_119.Instance);
            DirectConsume(in ManyNotifyEvent_119.Instance);
            DirectConsume(in ManyNotifyEvent_120.Instance);
            DirectConsume(in ManyNotifyEvent_120.Instance);
            DirectConsume(in ManyNotifyEvent_121.Instance);
            DirectConsume(in ManyNotifyEvent_121.Instance);
            DirectConsume(in ManyNotifyEvent_122.Instance);
            DirectConsume(in ManyNotifyEvent_122.Instance);
            DirectConsume(in ManyNotifyEvent_123.Instance);
            DirectConsume(in ManyNotifyEvent_123.Instance);
            DirectConsume(in ManyNotifyEvent_124.Instance);
            DirectConsume(in ManyNotifyEvent_124.Instance);
            DirectConsume(in ManyNotifyEvent_125.Instance);
            DirectConsume(in ManyNotifyEvent_125.Instance);
            DirectConsume(in ManyNotifyEvent_126.Instance);
            DirectConsume(in ManyNotifyEvent_126.Instance);
            DirectConsume(in ManyNotifyEvent_127.Instance);
            DirectConsume(in ManyNotifyEvent_127.Instance);
            DirectConsume(in ManyNotifyEvent_128.Instance);
            DirectConsume(in ManyNotifyEvent_128.Instance);
            DirectConsume(in ManyNotifyEvent_129.Instance);
            DirectConsume(in ManyNotifyEvent_129.Instance);
            DirectConsume(in ManyNotifyEvent_130.Instance);
            DirectConsume(in ManyNotifyEvent_130.Instance);
            DirectConsume(in ManyNotifyEvent_131.Instance);
            DirectConsume(in ManyNotifyEvent_131.Instance);
            DirectConsume(in ManyNotifyEvent_132.Instance);
            DirectConsume(in ManyNotifyEvent_132.Instance);
            DirectConsume(in ManyNotifyEvent_133.Instance);
            DirectConsume(in ManyNotifyEvent_133.Instance);
            DirectConsume(in ManyNotifyEvent_134.Instance);
            DirectConsume(in ManyNotifyEvent_134.Instance);
            DirectConsume(in ManyNotifyEvent_135.Instance);
            DirectConsume(in ManyNotifyEvent_135.Instance);
            DirectConsume(in ManyNotifyEvent_136.Instance);
            DirectConsume(in ManyNotifyEvent_136.Instance);
            DirectConsume(in ManyNotifyEvent_137.Instance);
            DirectConsume(in ManyNotifyEvent_137.Instance);
            DirectConsume(in ManyNotifyEvent_138.Instance);
            DirectConsume(in ManyNotifyEvent_138.Instance);
            DirectConsume(in ManyNotifyEvent_139.Instance);
            DirectConsume(in ManyNotifyEvent_139.Instance);
            DirectConsume(in ManyNotifyEvent_140.Instance);
            DirectConsume(in ManyNotifyEvent_140.Instance);
            DirectConsume(in ManyNotifyEvent_141.Instance);
            DirectConsume(in ManyNotifyEvent_141.Instance);
            DirectConsume(in ManyNotifyEvent_142.Instance);
            DirectConsume(in ManyNotifyEvent_142.Instance);
            DirectConsume(in ManyNotifyEvent_143.Instance);
            DirectConsume(in ManyNotifyEvent_143.Instance);
            DirectConsume(in ManyNotifyEvent_144.Instance);
            DirectConsume(in ManyNotifyEvent_144.Instance);
            DirectConsume(in ManyNotifyEvent_145.Instance);
            DirectConsume(in ManyNotifyEvent_145.Instance);
            DirectConsume(in ManyNotifyEvent_146.Instance);
            DirectConsume(in ManyNotifyEvent_146.Instance);
            DirectConsume(in ManyNotifyEvent_147.Instance);
            DirectConsume(in ManyNotifyEvent_147.Instance);
            DirectConsume(in ManyNotifyEvent_148.Instance);
            DirectConsume(in ManyNotifyEvent_148.Instance);
            DirectConsume(in ManyNotifyEvent_149.Instance);
            DirectConsume(in ManyNotifyEvent_149.Instance);
            DirectConsume(in ManyNotifyEvent_150.Instance);
            DirectConsume(in ManyNotifyEvent_150.Instance);
            DirectConsume(in ManyNotifyEvent_151.Instance);
            DirectConsume(in ManyNotifyEvent_151.Instance);
            DirectConsume(in ManyNotifyEvent_152.Instance);
            DirectConsume(in ManyNotifyEvent_152.Instance);
            DirectConsume(in ManyNotifyEvent_153.Instance);
            DirectConsume(in ManyNotifyEvent_153.Instance);
            DirectConsume(in ManyNotifyEvent_154.Instance);
            DirectConsume(in ManyNotifyEvent_154.Instance);
            DirectConsume(in ManyNotifyEvent_155.Instance);
            DirectConsume(in ManyNotifyEvent_155.Instance);
            DirectConsume(in ManyNotifyEvent_156.Instance);
            DirectConsume(in ManyNotifyEvent_156.Instance);
            DirectConsume(in ManyNotifyEvent_157.Instance);
            DirectConsume(in ManyNotifyEvent_157.Instance);
            DirectConsume(in ManyNotifyEvent_158.Instance);
            DirectConsume(in ManyNotifyEvent_158.Instance);
            DirectConsume(in ManyNotifyEvent_159.Instance);
            DirectConsume(in ManyNotifyEvent_159.Instance);
            DirectConsume(in ManyNotifyEvent_160.Instance);
            DirectConsume(in ManyNotifyEvent_160.Instance);
            DirectConsume(in ManyNotifyEvent_161.Instance);
            DirectConsume(in ManyNotifyEvent_161.Instance);
            DirectConsume(in ManyNotifyEvent_162.Instance);
            DirectConsume(in ManyNotifyEvent_162.Instance);
            DirectConsume(in ManyNotifyEvent_163.Instance);
            DirectConsume(in ManyNotifyEvent_163.Instance);
            DirectConsume(in ManyNotifyEvent_164.Instance);
            DirectConsume(in ManyNotifyEvent_164.Instance);
            DirectConsume(in ManyNotifyEvent_165.Instance);
            DirectConsume(in ManyNotifyEvent_165.Instance);
            DirectConsume(in ManyNotifyEvent_166.Instance);
            DirectConsume(in ManyNotifyEvent_166.Instance);
            DirectConsume(in ManyNotifyEvent_167.Instance);
            DirectConsume(in ManyNotifyEvent_167.Instance);
            DirectConsume(in ManyNotifyEvent_168.Instance);
            DirectConsume(in ManyNotifyEvent_168.Instance);
            DirectConsume(in ManyNotifyEvent_169.Instance);
            DirectConsume(in ManyNotifyEvent_169.Instance);
            DirectConsume(in ManyNotifyEvent_170.Instance);
            DirectConsume(in ManyNotifyEvent_170.Instance);
            DirectConsume(in ManyNotifyEvent_171.Instance);
            DirectConsume(in ManyNotifyEvent_171.Instance);
            DirectConsume(in ManyNotifyEvent_172.Instance);
            DirectConsume(in ManyNotifyEvent_172.Instance);
            DirectConsume(in ManyNotifyEvent_173.Instance);
            DirectConsume(in ManyNotifyEvent_173.Instance);
            DirectConsume(in ManyNotifyEvent_174.Instance);
            DirectConsume(in ManyNotifyEvent_174.Instance);
            DirectConsume(in ManyNotifyEvent_175.Instance);
            DirectConsume(in ManyNotifyEvent_175.Instance);
            DirectConsume(in ManyNotifyEvent_176.Instance);
            DirectConsume(in ManyNotifyEvent_176.Instance);
            DirectConsume(in ManyNotifyEvent_177.Instance);
            DirectConsume(in ManyNotifyEvent_177.Instance);
            DirectConsume(in ManyNotifyEvent_178.Instance);
            DirectConsume(in ManyNotifyEvent_178.Instance);
            DirectConsume(in ManyNotifyEvent_179.Instance);
            DirectConsume(in ManyNotifyEvent_179.Instance);
            DirectConsume(in ManyNotifyEvent_180.Instance);
            DirectConsume(in ManyNotifyEvent_180.Instance);
            DirectConsume(in ManyNotifyEvent_181.Instance);
            DirectConsume(in ManyNotifyEvent_181.Instance);
            DirectConsume(in ManyNotifyEvent_182.Instance);
            DirectConsume(in ManyNotifyEvent_182.Instance);
            DirectConsume(in ManyNotifyEvent_183.Instance);
            DirectConsume(in ManyNotifyEvent_183.Instance);
            DirectConsume(in ManyNotifyEvent_184.Instance);
            DirectConsume(in ManyNotifyEvent_184.Instance);
            DirectConsume(in ManyNotifyEvent_185.Instance);
            DirectConsume(in ManyNotifyEvent_185.Instance);
            DirectConsume(in ManyNotifyEvent_186.Instance);
            DirectConsume(in ManyNotifyEvent_186.Instance);
            DirectConsume(in ManyNotifyEvent_187.Instance);
            DirectConsume(in ManyNotifyEvent_187.Instance);
            DirectConsume(in ManyNotifyEvent_188.Instance);
            DirectConsume(in ManyNotifyEvent_188.Instance);
            DirectConsume(in ManyNotifyEvent_189.Instance);
            DirectConsume(in ManyNotifyEvent_189.Instance);
            DirectConsume(in ManyNotifyEvent_190.Instance);
            DirectConsume(in ManyNotifyEvent_190.Instance);
            DirectConsume(in ManyNotifyEvent_191.Instance);
            DirectConsume(in ManyNotifyEvent_191.Instance);
            DirectConsume(in ManyNotifyEvent_192.Instance);
            DirectConsume(in ManyNotifyEvent_192.Instance);
            DirectConsume(in ManyNotifyEvent_193.Instance);
            DirectConsume(in ManyNotifyEvent_193.Instance);
            DirectConsume(in ManyNotifyEvent_194.Instance);
            DirectConsume(in ManyNotifyEvent_194.Instance);
            DirectConsume(in ManyNotifyEvent_195.Instance);
            DirectConsume(in ManyNotifyEvent_195.Instance);
            DirectConsume(in ManyNotifyEvent_196.Instance);
            DirectConsume(in ManyNotifyEvent_196.Instance);
            DirectConsume(in ManyNotifyEvent_197.Instance);
            DirectConsume(in ManyNotifyEvent_197.Instance);
            DirectConsume(in ManyNotifyEvent_198.Instance);
            DirectConsume(in ManyNotifyEvent_198.Instance);
            DirectConsume(in ManyNotifyEvent_199.Instance);
            DirectConsume(in ManyNotifyEvent_199.Instance);
            DirectConsume(in ManyNotifyEvent_200.Instance);
            DirectConsume(in ManyNotifyEvent_200.Instance);
            DirectConsume(in ManyNotifyEvent_201.Instance);
            DirectConsume(in ManyNotifyEvent_201.Instance);
            DirectConsume(in ManyNotifyEvent_202.Instance);
            DirectConsume(in ManyNotifyEvent_202.Instance);
            DirectConsume(in ManyNotifyEvent_203.Instance);
            DirectConsume(in ManyNotifyEvent_203.Instance);
            DirectConsume(in ManyNotifyEvent_204.Instance);
            DirectConsume(in ManyNotifyEvent_204.Instance);
            DirectConsume(in ManyNotifyEvent_205.Instance);
            DirectConsume(in ManyNotifyEvent_205.Instance);
            DirectConsume(in ManyNotifyEvent_206.Instance);
            DirectConsume(in ManyNotifyEvent_206.Instance);
            DirectConsume(in ManyNotifyEvent_207.Instance);
            DirectConsume(in ManyNotifyEvent_207.Instance);
            DirectConsume(in ManyNotifyEvent_208.Instance);
            DirectConsume(in ManyNotifyEvent_208.Instance);
            DirectConsume(in ManyNotifyEvent_209.Instance);
            DirectConsume(in ManyNotifyEvent_209.Instance);
            DirectConsume(in ManyNotifyEvent_210.Instance);
            DirectConsume(in ManyNotifyEvent_210.Instance);
            DirectConsume(in ManyNotifyEvent_211.Instance);
            DirectConsume(in ManyNotifyEvent_211.Instance);
            DirectConsume(in ManyNotifyEvent_212.Instance);
            DirectConsume(in ManyNotifyEvent_212.Instance);
            DirectConsume(in ManyNotifyEvent_213.Instance);
            DirectConsume(in ManyNotifyEvent_213.Instance);
            DirectConsume(in ManyNotifyEvent_214.Instance);
            DirectConsume(in ManyNotifyEvent_214.Instance);
            DirectConsume(in ManyNotifyEvent_215.Instance);
            DirectConsume(in ManyNotifyEvent_215.Instance);
            DirectConsume(in ManyNotifyEvent_216.Instance);
            DirectConsume(in ManyNotifyEvent_216.Instance);
            DirectConsume(in ManyNotifyEvent_217.Instance);
            DirectConsume(in ManyNotifyEvent_217.Instance);
            DirectConsume(in ManyNotifyEvent_218.Instance);
            DirectConsume(in ManyNotifyEvent_218.Instance);
            DirectConsume(in ManyNotifyEvent_219.Instance);
            DirectConsume(in ManyNotifyEvent_219.Instance);
            DirectConsume(in ManyNotifyEvent_220.Instance);
            DirectConsume(in ManyNotifyEvent_220.Instance);
            DirectConsume(in ManyNotifyEvent_221.Instance);
            DirectConsume(in ManyNotifyEvent_221.Instance);
            DirectConsume(in ManyNotifyEvent_222.Instance);
            DirectConsume(in ManyNotifyEvent_222.Instance);
            DirectConsume(in ManyNotifyEvent_223.Instance);
            DirectConsume(in ManyNotifyEvent_223.Instance);
            DirectConsume(in ManyNotifyEvent_224.Instance);
            DirectConsume(in ManyNotifyEvent_224.Instance);
            DirectConsume(in ManyNotifyEvent_225.Instance);
            DirectConsume(in ManyNotifyEvent_225.Instance);
            DirectConsume(in ManyNotifyEvent_226.Instance);
            DirectConsume(in ManyNotifyEvent_226.Instance);
            DirectConsume(in ManyNotifyEvent_227.Instance);
            DirectConsume(in ManyNotifyEvent_227.Instance);
            DirectConsume(in ManyNotifyEvent_228.Instance);
            DirectConsume(in ManyNotifyEvent_228.Instance);
            DirectConsume(in ManyNotifyEvent_229.Instance);
            DirectConsume(in ManyNotifyEvent_229.Instance);
            DirectConsume(in ManyNotifyEvent_230.Instance);
            DirectConsume(in ManyNotifyEvent_230.Instance);
            DirectConsume(in ManyNotifyEvent_231.Instance);
            DirectConsume(in ManyNotifyEvent_231.Instance);
            DirectConsume(in ManyNotifyEvent_232.Instance);
            DirectConsume(in ManyNotifyEvent_232.Instance);
            DirectConsume(in ManyNotifyEvent_233.Instance);
            DirectConsume(in ManyNotifyEvent_233.Instance);
            DirectConsume(in ManyNotifyEvent_234.Instance);
            DirectConsume(in ManyNotifyEvent_234.Instance);
            DirectConsume(in ManyNotifyEvent_235.Instance);
            DirectConsume(in ManyNotifyEvent_235.Instance);
            DirectConsume(in ManyNotifyEvent_236.Instance);
            DirectConsume(in ManyNotifyEvent_236.Instance);
            DirectConsume(in ManyNotifyEvent_237.Instance);
            DirectConsume(in ManyNotifyEvent_237.Instance);
            DirectConsume(in ManyNotifyEvent_238.Instance);
            DirectConsume(in ManyNotifyEvent_238.Instance);
            DirectConsume(in ManyNotifyEvent_239.Instance);
            DirectConsume(in ManyNotifyEvent_239.Instance);
            DirectConsume(in ManyNotifyEvent_240.Instance);
            DirectConsume(in ManyNotifyEvent_240.Instance);
            DirectConsume(in ManyNotifyEvent_241.Instance);
            DirectConsume(in ManyNotifyEvent_241.Instance);
            DirectConsume(in ManyNotifyEvent_242.Instance);
            DirectConsume(in ManyNotifyEvent_242.Instance);
            DirectConsume(in ManyNotifyEvent_243.Instance);
            DirectConsume(in ManyNotifyEvent_243.Instance);
            DirectConsume(in ManyNotifyEvent_244.Instance);
            DirectConsume(in ManyNotifyEvent_244.Instance);
            DirectConsume(in ManyNotifyEvent_245.Instance);
            DirectConsume(in ManyNotifyEvent_245.Instance);
            DirectConsume(in ManyNotifyEvent_246.Instance);
            DirectConsume(in ManyNotifyEvent_246.Instance);
            DirectConsume(in ManyNotifyEvent_247.Instance);
            DirectConsume(in ManyNotifyEvent_247.Instance);
            DirectConsume(in ManyNotifyEvent_248.Instance);
            DirectConsume(in ManyNotifyEvent_248.Instance);
            DirectConsume(in ManyNotifyEvent_249.Instance);
            DirectConsume(in ManyNotifyEvent_249.Instance);
            DirectConsume(in ManyNotifyEvent_250.Instance);
            DirectConsume(in ManyNotifyEvent_250.Instance);
            DirectConsume(in ManyNotifyEvent_251.Instance);
            DirectConsume(in ManyNotifyEvent_251.Instance);
            DirectConsume(in ManyNotifyEvent_252.Instance);
            DirectConsume(in ManyNotifyEvent_252.Instance);
            DirectConsume(in ManyNotifyEvent_253.Instance);
            DirectConsume(in ManyNotifyEvent_253.Instance);
            DirectConsume(in ManyNotifyEvent_254.Instance);
            DirectConsume(in ManyNotifyEvent_254.Instance);
            DirectConsume(in ManyNotifyEvent_255.Instance);
            DirectConsume(in ManyNotifyEvent_255.Instance);
            return;
        }

        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_000.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_001.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_002.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_003.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_004.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_005.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_006.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_007.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_008.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_009.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_010.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_011.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_012.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_013.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_014.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_015.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_016.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_017.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_018.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_019.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_020.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_021.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_022.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_023.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_024.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_025.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_026.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_027.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_028.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_029.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_030.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_031.Instance);
        DirectConsume(in ManyNotifyEvent_032.Instance);
        DirectConsume(in ManyNotifyEvent_032.Instance);
        DirectConsume(in ManyNotifyEvent_032.Instance);
        DirectConsume(in ManyNotifyEvent_033.Instance);
        DirectConsume(in ManyNotifyEvent_033.Instance);
        DirectConsume(in ManyNotifyEvent_033.Instance);
        DirectConsume(in ManyNotifyEvent_034.Instance);
        DirectConsume(in ManyNotifyEvent_034.Instance);
        DirectConsume(in ManyNotifyEvent_034.Instance);
        DirectConsume(in ManyNotifyEvent_035.Instance);
        DirectConsume(in ManyNotifyEvent_035.Instance);
        DirectConsume(in ManyNotifyEvent_035.Instance);
        DirectConsume(in ManyNotifyEvent_036.Instance);
        DirectConsume(in ManyNotifyEvent_036.Instance);
        DirectConsume(in ManyNotifyEvent_036.Instance);
        DirectConsume(in ManyNotifyEvent_037.Instance);
        DirectConsume(in ManyNotifyEvent_037.Instance);
        DirectConsume(in ManyNotifyEvent_037.Instance);
        DirectConsume(in ManyNotifyEvent_038.Instance);
        DirectConsume(in ManyNotifyEvent_038.Instance);
        DirectConsume(in ManyNotifyEvent_038.Instance);
        DirectConsume(in ManyNotifyEvent_039.Instance);
        DirectConsume(in ManyNotifyEvent_039.Instance);
        DirectConsume(in ManyNotifyEvent_039.Instance);
        DirectConsume(in ManyNotifyEvent_040.Instance);
        DirectConsume(in ManyNotifyEvent_040.Instance);
        DirectConsume(in ManyNotifyEvent_040.Instance);
        DirectConsume(in ManyNotifyEvent_041.Instance);
        DirectConsume(in ManyNotifyEvent_041.Instance);
        DirectConsume(in ManyNotifyEvent_041.Instance);
        DirectConsume(in ManyNotifyEvent_042.Instance);
        DirectConsume(in ManyNotifyEvent_042.Instance);
        DirectConsume(in ManyNotifyEvent_042.Instance);
        DirectConsume(in ManyNotifyEvent_043.Instance);
        DirectConsume(in ManyNotifyEvent_043.Instance);
        DirectConsume(in ManyNotifyEvent_043.Instance);
        DirectConsume(in ManyNotifyEvent_044.Instance);
        DirectConsume(in ManyNotifyEvent_044.Instance);
        DirectConsume(in ManyNotifyEvent_044.Instance);
        DirectConsume(in ManyNotifyEvent_045.Instance);
        DirectConsume(in ManyNotifyEvent_045.Instance);
        DirectConsume(in ManyNotifyEvent_045.Instance);
        DirectConsume(in ManyNotifyEvent_046.Instance);
        DirectConsume(in ManyNotifyEvent_046.Instance);
        DirectConsume(in ManyNotifyEvent_046.Instance);
        DirectConsume(in ManyNotifyEvent_047.Instance);
        DirectConsume(in ManyNotifyEvent_047.Instance);
        DirectConsume(in ManyNotifyEvent_047.Instance);
        DirectConsume(in ManyNotifyEvent_048.Instance);
        DirectConsume(in ManyNotifyEvent_048.Instance);
        DirectConsume(in ManyNotifyEvent_048.Instance);
        DirectConsume(in ManyNotifyEvent_049.Instance);
        DirectConsume(in ManyNotifyEvent_049.Instance);
        DirectConsume(in ManyNotifyEvent_049.Instance);
        DirectConsume(in ManyNotifyEvent_050.Instance);
        DirectConsume(in ManyNotifyEvent_050.Instance);
        DirectConsume(in ManyNotifyEvent_050.Instance);
        DirectConsume(in ManyNotifyEvent_051.Instance);
        DirectConsume(in ManyNotifyEvent_051.Instance);
        DirectConsume(in ManyNotifyEvent_051.Instance);
        DirectConsume(in ManyNotifyEvent_052.Instance);
        DirectConsume(in ManyNotifyEvent_052.Instance);
        DirectConsume(in ManyNotifyEvent_052.Instance);
        DirectConsume(in ManyNotifyEvent_053.Instance);
        DirectConsume(in ManyNotifyEvent_053.Instance);
        DirectConsume(in ManyNotifyEvent_053.Instance);
        DirectConsume(in ManyNotifyEvent_054.Instance);
        DirectConsume(in ManyNotifyEvent_054.Instance);
        DirectConsume(in ManyNotifyEvent_054.Instance);
        DirectConsume(in ManyNotifyEvent_055.Instance);
        DirectConsume(in ManyNotifyEvent_055.Instance);
        DirectConsume(in ManyNotifyEvent_055.Instance);
        DirectConsume(in ManyNotifyEvent_056.Instance);
        DirectConsume(in ManyNotifyEvent_056.Instance);
        DirectConsume(in ManyNotifyEvent_056.Instance);
        DirectConsume(in ManyNotifyEvent_057.Instance);
        DirectConsume(in ManyNotifyEvent_057.Instance);
        DirectConsume(in ManyNotifyEvent_057.Instance);
        DirectConsume(in ManyNotifyEvent_058.Instance);
        DirectConsume(in ManyNotifyEvent_058.Instance);
        DirectConsume(in ManyNotifyEvent_058.Instance);
        DirectConsume(in ManyNotifyEvent_059.Instance);
        DirectConsume(in ManyNotifyEvent_059.Instance);
        DirectConsume(in ManyNotifyEvent_059.Instance);
        DirectConsume(in ManyNotifyEvent_060.Instance);
        DirectConsume(in ManyNotifyEvent_060.Instance);
        DirectConsume(in ManyNotifyEvent_060.Instance);
        DirectConsume(in ManyNotifyEvent_061.Instance);
        DirectConsume(in ManyNotifyEvent_061.Instance);
        DirectConsume(in ManyNotifyEvent_061.Instance);
        DirectConsume(in ManyNotifyEvent_062.Instance);
        DirectConsume(in ManyNotifyEvent_062.Instance);
        DirectConsume(in ManyNotifyEvent_062.Instance);
        DirectConsume(in ManyNotifyEvent_063.Instance);
        DirectConsume(in ManyNotifyEvent_063.Instance);
        DirectConsume(in ManyNotifyEvent_063.Instance);
        DirectConsume(in ManyNotifyEvent_064.Instance);
        DirectConsume(in ManyNotifyEvent_064.Instance);
        DirectConsume(in ManyNotifyEvent_064.Instance);
        DirectConsume(in ManyNotifyEvent_065.Instance);
        DirectConsume(in ManyNotifyEvent_065.Instance);
        DirectConsume(in ManyNotifyEvent_065.Instance);
        DirectConsume(in ManyNotifyEvent_066.Instance);
        DirectConsume(in ManyNotifyEvent_066.Instance);
        DirectConsume(in ManyNotifyEvent_066.Instance);
        DirectConsume(in ManyNotifyEvent_067.Instance);
        DirectConsume(in ManyNotifyEvent_067.Instance);
        DirectConsume(in ManyNotifyEvent_067.Instance);
        DirectConsume(in ManyNotifyEvent_068.Instance);
        DirectConsume(in ManyNotifyEvent_068.Instance);
        DirectConsume(in ManyNotifyEvent_068.Instance);
        DirectConsume(in ManyNotifyEvent_069.Instance);
        DirectConsume(in ManyNotifyEvent_069.Instance);
        DirectConsume(in ManyNotifyEvent_069.Instance);
        DirectConsume(in ManyNotifyEvent_070.Instance);
        DirectConsume(in ManyNotifyEvent_070.Instance);
        DirectConsume(in ManyNotifyEvent_070.Instance);
        DirectConsume(in ManyNotifyEvent_071.Instance);
        DirectConsume(in ManyNotifyEvent_071.Instance);
        DirectConsume(in ManyNotifyEvent_071.Instance);
        DirectConsume(in ManyNotifyEvent_072.Instance);
        DirectConsume(in ManyNotifyEvent_072.Instance);
        DirectConsume(in ManyNotifyEvent_072.Instance);
        DirectConsume(in ManyNotifyEvent_073.Instance);
        DirectConsume(in ManyNotifyEvent_073.Instance);
        DirectConsume(in ManyNotifyEvent_073.Instance);
        DirectConsume(in ManyNotifyEvent_074.Instance);
        DirectConsume(in ManyNotifyEvent_074.Instance);
        DirectConsume(in ManyNotifyEvent_074.Instance);
        DirectConsume(in ManyNotifyEvent_075.Instance);
        DirectConsume(in ManyNotifyEvent_075.Instance);
        DirectConsume(in ManyNotifyEvent_075.Instance);
        DirectConsume(in ManyNotifyEvent_076.Instance);
        DirectConsume(in ManyNotifyEvent_076.Instance);
        DirectConsume(in ManyNotifyEvent_076.Instance);
        DirectConsume(in ManyNotifyEvent_077.Instance);
        DirectConsume(in ManyNotifyEvent_077.Instance);
        DirectConsume(in ManyNotifyEvent_077.Instance);
        DirectConsume(in ManyNotifyEvent_078.Instance);
        DirectConsume(in ManyNotifyEvent_078.Instance);
        DirectConsume(in ManyNotifyEvent_078.Instance);
        DirectConsume(in ManyNotifyEvent_079.Instance);
        DirectConsume(in ManyNotifyEvent_079.Instance);
        DirectConsume(in ManyNotifyEvent_079.Instance);
        DirectConsume(in ManyNotifyEvent_080.Instance);
        DirectConsume(in ManyNotifyEvent_080.Instance);
        DirectConsume(in ManyNotifyEvent_080.Instance);
        DirectConsume(in ManyNotifyEvent_081.Instance);
        DirectConsume(in ManyNotifyEvent_081.Instance);
        DirectConsume(in ManyNotifyEvent_081.Instance);
        DirectConsume(in ManyNotifyEvent_082.Instance);
        DirectConsume(in ManyNotifyEvent_082.Instance);
        DirectConsume(in ManyNotifyEvent_082.Instance);
        DirectConsume(in ManyNotifyEvent_083.Instance);
        DirectConsume(in ManyNotifyEvent_083.Instance);
        DirectConsume(in ManyNotifyEvent_083.Instance);
        DirectConsume(in ManyNotifyEvent_084.Instance);
        DirectConsume(in ManyNotifyEvent_084.Instance);
        DirectConsume(in ManyNotifyEvent_084.Instance);
        DirectConsume(in ManyNotifyEvent_085.Instance);
        DirectConsume(in ManyNotifyEvent_085.Instance);
        DirectConsume(in ManyNotifyEvent_085.Instance);
        DirectConsume(in ManyNotifyEvent_086.Instance);
        DirectConsume(in ManyNotifyEvent_086.Instance);
        DirectConsume(in ManyNotifyEvent_086.Instance);
        DirectConsume(in ManyNotifyEvent_087.Instance);
        DirectConsume(in ManyNotifyEvent_087.Instance);
        DirectConsume(in ManyNotifyEvent_087.Instance);
        DirectConsume(in ManyNotifyEvent_088.Instance);
        DirectConsume(in ManyNotifyEvent_088.Instance);
        DirectConsume(in ManyNotifyEvent_088.Instance);
        DirectConsume(in ManyNotifyEvent_089.Instance);
        DirectConsume(in ManyNotifyEvent_089.Instance);
        DirectConsume(in ManyNotifyEvent_089.Instance);
        DirectConsume(in ManyNotifyEvent_090.Instance);
        DirectConsume(in ManyNotifyEvent_090.Instance);
        DirectConsume(in ManyNotifyEvent_090.Instance);
        DirectConsume(in ManyNotifyEvent_091.Instance);
        DirectConsume(in ManyNotifyEvent_091.Instance);
        DirectConsume(in ManyNotifyEvent_091.Instance);
        DirectConsume(in ManyNotifyEvent_092.Instance);
        DirectConsume(in ManyNotifyEvent_092.Instance);
        DirectConsume(in ManyNotifyEvent_092.Instance);
        DirectConsume(in ManyNotifyEvent_093.Instance);
        DirectConsume(in ManyNotifyEvent_093.Instance);
        DirectConsume(in ManyNotifyEvent_093.Instance);
        DirectConsume(in ManyNotifyEvent_094.Instance);
        DirectConsume(in ManyNotifyEvent_094.Instance);
        DirectConsume(in ManyNotifyEvent_094.Instance);
        DirectConsume(in ManyNotifyEvent_095.Instance);
        DirectConsume(in ManyNotifyEvent_095.Instance);
        DirectConsume(in ManyNotifyEvent_095.Instance);
        DirectConsume(in ManyNotifyEvent_096.Instance);
        DirectConsume(in ManyNotifyEvent_096.Instance);
        DirectConsume(in ManyNotifyEvent_096.Instance);
        DirectConsume(in ManyNotifyEvent_097.Instance);
        DirectConsume(in ManyNotifyEvent_097.Instance);
        DirectConsume(in ManyNotifyEvent_097.Instance);
        DirectConsume(in ManyNotifyEvent_098.Instance);
        DirectConsume(in ManyNotifyEvent_098.Instance);
        DirectConsume(in ManyNotifyEvent_098.Instance);
        DirectConsume(in ManyNotifyEvent_099.Instance);
        DirectConsume(in ManyNotifyEvent_099.Instance);
        DirectConsume(in ManyNotifyEvent_099.Instance);
        DirectConsume(in ManyNotifyEvent_100.Instance);
        DirectConsume(in ManyNotifyEvent_100.Instance);
        DirectConsume(in ManyNotifyEvent_100.Instance);
        DirectConsume(in ManyNotifyEvent_101.Instance);
        DirectConsume(in ManyNotifyEvent_101.Instance);
        DirectConsume(in ManyNotifyEvent_101.Instance);
        DirectConsume(in ManyNotifyEvent_102.Instance);
        DirectConsume(in ManyNotifyEvent_102.Instance);
        DirectConsume(in ManyNotifyEvent_102.Instance);
        DirectConsume(in ManyNotifyEvent_103.Instance);
        DirectConsume(in ManyNotifyEvent_103.Instance);
        DirectConsume(in ManyNotifyEvent_103.Instance);
        DirectConsume(in ManyNotifyEvent_104.Instance);
        DirectConsume(in ManyNotifyEvent_104.Instance);
        DirectConsume(in ManyNotifyEvent_104.Instance);
        DirectConsume(in ManyNotifyEvent_105.Instance);
        DirectConsume(in ManyNotifyEvent_105.Instance);
        DirectConsume(in ManyNotifyEvent_105.Instance);
        DirectConsume(in ManyNotifyEvent_106.Instance);
        DirectConsume(in ManyNotifyEvent_106.Instance);
        DirectConsume(in ManyNotifyEvent_106.Instance);
        DirectConsume(in ManyNotifyEvent_107.Instance);
        DirectConsume(in ManyNotifyEvent_107.Instance);
        DirectConsume(in ManyNotifyEvent_107.Instance);
        DirectConsume(in ManyNotifyEvent_108.Instance);
        DirectConsume(in ManyNotifyEvent_108.Instance);
        DirectConsume(in ManyNotifyEvent_108.Instance);
        DirectConsume(in ManyNotifyEvent_109.Instance);
        DirectConsume(in ManyNotifyEvent_109.Instance);
        DirectConsume(in ManyNotifyEvent_109.Instance);
        DirectConsume(in ManyNotifyEvent_110.Instance);
        DirectConsume(in ManyNotifyEvent_110.Instance);
        DirectConsume(in ManyNotifyEvent_110.Instance);
        DirectConsume(in ManyNotifyEvent_111.Instance);
        DirectConsume(in ManyNotifyEvent_111.Instance);
        DirectConsume(in ManyNotifyEvent_111.Instance);
        DirectConsume(in ManyNotifyEvent_112.Instance);
        DirectConsume(in ManyNotifyEvent_112.Instance);
        DirectConsume(in ManyNotifyEvent_112.Instance);
        DirectConsume(in ManyNotifyEvent_113.Instance);
        DirectConsume(in ManyNotifyEvent_113.Instance);
        DirectConsume(in ManyNotifyEvent_113.Instance);
        DirectConsume(in ManyNotifyEvent_114.Instance);
        DirectConsume(in ManyNotifyEvent_114.Instance);
        DirectConsume(in ManyNotifyEvent_114.Instance);
        DirectConsume(in ManyNotifyEvent_115.Instance);
        DirectConsume(in ManyNotifyEvent_115.Instance);
        DirectConsume(in ManyNotifyEvent_115.Instance);
        DirectConsume(in ManyNotifyEvent_116.Instance);
        DirectConsume(in ManyNotifyEvent_116.Instance);
        DirectConsume(in ManyNotifyEvent_116.Instance);
        DirectConsume(in ManyNotifyEvent_117.Instance);
        DirectConsume(in ManyNotifyEvent_117.Instance);
        DirectConsume(in ManyNotifyEvent_117.Instance);
        DirectConsume(in ManyNotifyEvent_118.Instance);
        DirectConsume(in ManyNotifyEvent_118.Instance);
        DirectConsume(in ManyNotifyEvent_118.Instance);
        DirectConsume(in ManyNotifyEvent_119.Instance);
        DirectConsume(in ManyNotifyEvent_119.Instance);
        DirectConsume(in ManyNotifyEvent_119.Instance);
        DirectConsume(in ManyNotifyEvent_120.Instance);
        DirectConsume(in ManyNotifyEvent_120.Instance);
        DirectConsume(in ManyNotifyEvent_120.Instance);
        DirectConsume(in ManyNotifyEvent_121.Instance);
        DirectConsume(in ManyNotifyEvent_121.Instance);
        DirectConsume(in ManyNotifyEvent_121.Instance);
        DirectConsume(in ManyNotifyEvent_122.Instance);
        DirectConsume(in ManyNotifyEvent_122.Instance);
        DirectConsume(in ManyNotifyEvent_122.Instance);
        DirectConsume(in ManyNotifyEvent_123.Instance);
        DirectConsume(in ManyNotifyEvent_123.Instance);
        DirectConsume(in ManyNotifyEvent_123.Instance);
        DirectConsume(in ManyNotifyEvent_124.Instance);
        DirectConsume(in ManyNotifyEvent_124.Instance);
        DirectConsume(in ManyNotifyEvent_124.Instance);
        DirectConsume(in ManyNotifyEvent_125.Instance);
        DirectConsume(in ManyNotifyEvent_125.Instance);
        DirectConsume(in ManyNotifyEvent_125.Instance);
        DirectConsume(in ManyNotifyEvent_126.Instance);
        DirectConsume(in ManyNotifyEvent_126.Instance);
        DirectConsume(in ManyNotifyEvent_126.Instance);
        DirectConsume(in ManyNotifyEvent_127.Instance);
        DirectConsume(in ManyNotifyEvent_127.Instance);
        DirectConsume(in ManyNotifyEvent_127.Instance);
        DirectConsume(in ManyNotifyEvent_128.Instance);
        DirectConsume(in ManyNotifyEvent_128.Instance);
        DirectConsume(in ManyNotifyEvent_128.Instance);
        DirectConsume(in ManyNotifyEvent_129.Instance);
        DirectConsume(in ManyNotifyEvent_129.Instance);
        DirectConsume(in ManyNotifyEvent_129.Instance);
        DirectConsume(in ManyNotifyEvent_130.Instance);
        DirectConsume(in ManyNotifyEvent_130.Instance);
        DirectConsume(in ManyNotifyEvent_130.Instance);
        DirectConsume(in ManyNotifyEvent_131.Instance);
        DirectConsume(in ManyNotifyEvent_131.Instance);
        DirectConsume(in ManyNotifyEvent_131.Instance);
        DirectConsume(in ManyNotifyEvent_132.Instance);
        DirectConsume(in ManyNotifyEvent_132.Instance);
        DirectConsume(in ManyNotifyEvent_132.Instance);
        DirectConsume(in ManyNotifyEvent_133.Instance);
        DirectConsume(in ManyNotifyEvent_133.Instance);
        DirectConsume(in ManyNotifyEvent_133.Instance);
        DirectConsume(in ManyNotifyEvent_134.Instance);
        DirectConsume(in ManyNotifyEvent_134.Instance);
        DirectConsume(in ManyNotifyEvent_134.Instance);
        DirectConsume(in ManyNotifyEvent_135.Instance);
        DirectConsume(in ManyNotifyEvent_135.Instance);
        DirectConsume(in ManyNotifyEvent_135.Instance);
        DirectConsume(in ManyNotifyEvent_136.Instance);
        DirectConsume(in ManyNotifyEvent_136.Instance);
        DirectConsume(in ManyNotifyEvent_136.Instance);
        DirectConsume(in ManyNotifyEvent_137.Instance);
        DirectConsume(in ManyNotifyEvent_137.Instance);
        DirectConsume(in ManyNotifyEvent_137.Instance);
        DirectConsume(in ManyNotifyEvent_138.Instance);
        DirectConsume(in ManyNotifyEvent_138.Instance);
        DirectConsume(in ManyNotifyEvent_138.Instance);
        DirectConsume(in ManyNotifyEvent_139.Instance);
        DirectConsume(in ManyNotifyEvent_139.Instance);
        DirectConsume(in ManyNotifyEvent_139.Instance);
        DirectConsume(in ManyNotifyEvent_140.Instance);
        DirectConsume(in ManyNotifyEvent_140.Instance);
        DirectConsume(in ManyNotifyEvent_140.Instance);
        DirectConsume(in ManyNotifyEvent_141.Instance);
        DirectConsume(in ManyNotifyEvent_141.Instance);
        DirectConsume(in ManyNotifyEvent_141.Instance);
        DirectConsume(in ManyNotifyEvent_142.Instance);
        DirectConsume(in ManyNotifyEvent_142.Instance);
        DirectConsume(in ManyNotifyEvent_142.Instance);
        DirectConsume(in ManyNotifyEvent_143.Instance);
        DirectConsume(in ManyNotifyEvent_143.Instance);
        DirectConsume(in ManyNotifyEvent_143.Instance);
        DirectConsume(in ManyNotifyEvent_144.Instance);
        DirectConsume(in ManyNotifyEvent_144.Instance);
        DirectConsume(in ManyNotifyEvent_144.Instance);
        DirectConsume(in ManyNotifyEvent_145.Instance);
        DirectConsume(in ManyNotifyEvent_145.Instance);
        DirectConsume(in ManyNotifyEvent_145.Instance);
        DirectConsume(in ManyNotifyEvent_146.Instance);
        DirectConsume(in ManyNotifyEvent_146.Instance);
        DirectConsume(in ManyNotifyEvent_146.Instance);
        DirectConsume(in ManyNotifyEvent_147.Instance);
        DirectConsume(in ManyNotifyEvent_147.Instance);
        DirectConsume(in ManyNotifyEvent_147.Instance);
        DirectConsume(in ManyNotifyEvent_148.Instance);
        DirectConsume(in ManyNotifyEvent_148.Instance);
        DirectConsume(in ManyNotifyEvent_148.Instance);
        DirectConsume(in ManyNotifyEvent_149.Instance);
        DirectConsume(in ManyNotifyEvent_149.Instance);
        DirectConsume(in ManyNotifyEvent_149.Instance);
        DirectConsume(in ManyNotifyEvent_150.Instance);
        DirectConsume(in ManyNotifyEvent_150.Instance);
        DirectConsume(in ManyNotifyEvent_150.Instance);
        DirectConsume(in ManyNotifyEvent_151.Instance);
        DirectConsume(in ManyNotifyEvent_151.Instance);
        DirectConsume(in ManyNotifyEvent_151.Instance);
        DirectConsume(in ManyNotifyEvent_152.Instance);
        DirectConsume(in ManyNotifyEvent_152.Instance);
        DirectConsume(in ManyNotifyEvent_152.Instance);
        DirectConsume(in ManyNotifyEvent_153.Instance);
        DirectConsume(in ManyNotifyEvent_153.Instance);
        DirectConsume(in ManyNotifyEvent_153.Instance);
        DirectConsume(in ManyNotifyEvent_154.Instance);
        DirectConsume(in ManyNotifyEvent_154.Instance);
        DirectConsume(in ManyNotifyEvent_154.Instance);
        DirectConsume(in ManyNotifyEvent_155.Instance);
        DirectConsume(in ManyNotifyEvent_155.Instance);
        DirectConsume(in ManyNotifyEvent_155.Instance);
        DirectConsume(in ManyNotifyEvent_156.Instance);
        DirectConsume(in ManyNotifyEvent_156.Instance);
        DirectConsume(in ManyNotifyEvent_156.Instance);
        DirectConsume(in ManyNotifyEvent_157.Instance);
        DirectConsume(in ManyNotifyEvent_157.Instance);
        DirectConsume(in ManyNotifyEvent_157.Instance);
        DirectConsume(in ManyNotifyEvent_158.Instance);
        DirectConsume(in ManyNotifyEvent_158.Instance);
        DirectConsume(in ManyNotifyEvent_158.Instance);
        DirectConsume(in ManyNotifyEvent_159.Instance);
        DirectConsume(in ManyNotifyEvent_159.Instance);
        DirectConsume(in ManyNotifyEvent_159.Instance);
        DirectConsume(in ManyNotifyEvent_160.Instance);
        DirectConsume(in ManyNotifyEvent_160.Instance);
        DirectConsume(in ManyNotifyEvent_160.Instance);
        DirectConsume(in ManyNotifyEvent_161.Instance);
        DirectConsume(in ManyNotifyEvent_161.Instance);
        DirectConsume(in ManyNotifyEvent_161.Instance);
        DirectConsume(in ManyNotifyEvent_162.Instance);
        DirectConsume(in ManyNotifyEvent_162.Instance);
        DirectConsume(in ManyNotifyEvent_162.Instance);
        DirectConsume(in ManyNotifyEvent_163.Instance);
        DirectConsume(in ManyNotifyEvent_163.Instance);
        DirectConsume(in ManyNotifyEvent_163.Instance);
        DirectConsume(in ManyNotifyEvent_164.Instance);
        DirectConsume(in ManyNotifyEvent_164.Instance);
        DirectConsume(in ManyNotifyEvent_164.Instance);
        DirectConsume(in ManyNotifyEvent_165.Instance);
        DirectConsume(in ManyNotifyEvent_165.Instance);
        DirectConsume(in ManyNotifyEvent_165.Instance);
        DirectConsume(in ManyNotifyEvent_166.Instance);
        DirectConsume(in ManyNotifyEvent_166.Instance);
        DirectConsume(in ManyNotifyEvent_166.Instance);
        DirectConsume(in ManyNotifyEvent_167.Instance);
        DirectConsume(in ManyNotifyEvent_167.Instance);
        DirectConsume(in ManyNotifyEvent_167.Instance);
        DirectConsume(in ManyNotifyEvent_168.Instance);
        DirectConsume(in ManyNotifyEvent_168.Instance);
        DirectConsume(in ManyNotifyEvent_168.Instance);
        DirectConsume(in ManyNotifyEvent_169.Instance);
        DirectConsume(in ManyNotifyEvent_169.Instance);
        DirectConsume(in ManyNotifyEvent_169.Instance);
        DirectConsume(in ManyNotifyEvent_170.Instance);
        DirectConsume(in ManyNotifyEvent_170.Instance);
        DirectConsume(in ManyNotifyEvent_170.Instance);
        DirectConsume(in ManyNotifyEvent_171.Instance);
        DirectConsume(in ManyNotifyEvent_171.Instance);
        DirectConsume(in ManyNotifyEvent_171.Instance);
        DirectConsume(in ManyNotifyEvent_172.Instance);
        DirectConsume(in ManyNotifyEvent_172.Instance);
        DirectConsume(in ManyNotifyEvent_172.Instance);
        DirectConsume(in ManyNotifyEvent_173.Instance);
        DirectConsume(in ManyNotifyEvent_173.Instance);
        DirectConsume(in ManyNotifyEvent_173.Instance);
        DirectConsume(in ManyNotifyEvent_174.Instance);
        DirectConsume(in ManyNotifyEvent_174.Instance);
        DirectConsume(in ManyNotifyEvent_174.Instance);
        DirectConsume(in ManyNotifyEvent_175.Instance);
        DirectConsume(in ManyNotifyEvent_175.Instance);
        DirectConsume(in ManyNotifyEvent_175.Instance);
        DirectConsume(in ManyNotifyEvent_176.Instance);
        DirectConsume(in ManyNotifyEvent_176.Instance);
        DirectConsume(in ManyNotifyEvent_176.Instance);
        DirectConsume(in ManyNotifyEvent_177.Instance);
        DirectConsume(in ManyNotifyEvent_177.Instance);
        DirectConsume(in ManyNotifyEvent_177.Instance);
        DirectConsume(in ManyNotifyEvent_178.Instance);
        DirectConsume(in ManyNotifyEvent_178.Instance);
        DirectConsume(in ManyNotifyEvent_178.Instance);
        DirectConsume(in ManyNotifyEvent_179.Instance);
        DirectConsume(in ManyNotifyEvent_179.Instance);
        DirectConsume(in ManyNotifyEvent_179.Instance);
        DirectConsume(in ManyNotifyEvent_180.Instance);
        DirectConsume(in ManyNotifyEvent_180.Instance);
        DirectConsume(in ManyNotifyEvent_180.Instance);
        DirectConsume(in ManyNotifyEvent_181.Instance);
        DirectConsume(in ManyNotifyEvent_181.Instance);
        DirectConsume(in ManyNotifyEvent_181.Instance);
        DirectConsume(in ManyNotifyEvent_182.Instance);
        DirectConsume(in ManyNotifyEvent_182.Instance);
        DirectConsume(in ManyNotifyEvent_182.Instance);
        DirectConsume(in ManyNotifyEvent_183.Instance);
        DirectConsume(in ManyNotifyEvent_183.Instance);
        DirectConsume(in ManyNotifyEvent_183.Instance);
        DirectConsume(in ManyNotifyEvent_184.Instance);
        DirectConsume(in ManyNotifyEvent_184.Instance);
        DirectConsume(in ManyNotifyEvent_184.Instance);
        DirectConsume(in ManyNotifyEvent_185.Instance);
        DirectConsume(in ManyNotifyEvent_185.Instance);
        DirectConsume(in ManyNotifyEvent_185.Instance);
        DirectConsume(in ManyNotifyEvent_186.Instance);
        DirectConsume(in ManyNotifyEvent_186.Instance);
        DirectConsume(in ManyNotifyEvent_186.Instance);
        DirectConsume(in ManyNotifyEvent_187.Instance);
        DirectConsume(in ManyNotifyEvent_187.Instance);
        DirectConsume(in ManyNotifyEvent_187.Instance);
        DirectConsume(in ManyNotifyEvent_188.Instance);
        DirectConsume(in ManyNotifyEvent_188.Instance);
        DirectConsume(in ManyNotifyEvent_188.Instance);
        DirectConsume(in ManyNotifyEvent_189.Instance);
        DirectConsume(in ManyNotifyEvent_189.Instance);
        DirectConsume(in ManyNotifyEvent_189.Instance);
        DirectConsume(in ManyNotifyEvent_190.Instance);
        DirectConsume(in ManyNotifyEvent_190.Instance);
        DirectConsume(in ManyNotifyEvent_190.Instance);
        DirectConsume(in ManyNotifyEvent_191.Instance);
        DirectConsume(in ManyNotifyEvent_191.Instance);
        DirectConsume(in ManyNotifyEvent_191.Instance);
        DirectConsume(in ManyNotifyEvent_192.Instance);
        DirectConsume(in ManyNotifyEvent_192.Instance);
        DirectConsume(in ManyNotifyEvent_192.Instance);
        DirectConsume(in ManyNotifyEvent_193.Instance);
        DirectConsume(in ManyNotifyEvent_193.Instance);
        DirectConsume(in ManyNotifyEvent_193.Instance);
        DirectConsume(in ManyNotifyEvent_194.Instance);
        DirectConsume(in ManyNotifyEvent_194.Instance);
        DirectConsume(in ManyNotifyEvent_194.Instance);
        DirectConsume(in ManyNotifyEvent_195.Instance);
        DirectConsume(in ManyNotifyEvent_195.Instance);
        DirectConsume(in ManyNotifyEvent_195.Instance);
        DirectConsume(in ManyNotifyEvent_196.Instance);
        DirectConsume(in ManyNotifyEvent_196.Instance);
        DirectConsume(in ManyNotifyEvent_196.Instance);
        DirectConsume(in ManyNotifyEvent_197.Instance);
        DirectConsume(in ManyNotifyEvent_197.Instance);
        DirectConsume(in ManyNotifyEvent_197.Instance);
        DirectConsume(in ManyNotifyEvent_198.Instance);
        DirectConsume(in ManyNotifyEvent_198.Instance);
        DirectConsume(in ManyNotifyEvent_198.Instance);
        DirectConsume(in ManyNotifyEvent_199.Instance);
        DirectConsume(in ManyNotifyEvent_199.Instance);
        DirectConsume(in ManyNotifyEvent_199.Instance);
        DirectConsume(in ManyNotifyEvent_200.Instance);
        DirectConsume(in ManyNotifyEvent_200.Instance);
        DirectConsume(in ManyNotifyEvent_200.Instance);
        DirectConsume(in ManyNotifyEvent_201.Instance);
        DirectConsume(in ManyNotifyEvent_201.Instance);
        DirectConsume(in ManyNotifyEvent_201.Instance);
        DirectConsume(in ManyNotifyEvent_202.Instance);
        DirectConsume(in ManyNotifyEvent_202.Instance);
        DirectConsume(in ManyNotifyEvent_202.Instance);
        DirectConsume(in ManyNotifyEvent_203.Instance);
        DirectConsume(in ManyNotifyEvent_203.Instance);
        DirectConsume(in ManyNotifyEvent_203.Instance);
        DirectConsume(in ManyNotifyEvent_204.Instance);
        DirectConsume(in ManyNotifyEvent_204.Instance);
        DirectConsume(in ManyNotifyEvent_204.Instance);
        DirectConsume(in ManyNotifyEvent_205.Instance);
        DirectConsume(in ManyNotifyEvent_205.Instance);
        DirectConsume(in ManyNotifyEvent_205.Instance);
        DirectConsume(in ManyNotifyEvent_206.Instance);
        DirectConsume(in ManyNotifyEvent_206.Instance);
        DirectConsume(in ManyNotifyEvent_206.Instance);
        DirectConsume(in ManyNotifyEvent_207.Instance);
        DirectConsume(in ManyNotifyEvent_207.Instance);
        DirectConsume(in ManyNotifyEvent_207.Instance);
        DirectConsume(in ManyNotifyEvent_208.Instance);
        DirectConsume(in ManyNotifyEvent_208.Instance);
        DirectConsume(in ManyNotifyEvent_208.Instance);
        DirectConsume(in ManyNotifyEvent_209.Instance);
        DirectConsume(in ManyNotifyEvent_209.Instance);
        DirectConsume(in ManyNotifyEvent_209.Instance);
        DirectConsume(in ManyNotifyEvent_210.Instance);
        DirectConsume(in ManyNotifyEvent_210.Instance);
        DirectConsume(in ManyNotifyEvent_210.Instance);
        DirectConsume(in ManyNotifyEvent_211.Instance);
        DirectConsume(in ManyNotifyEvent_211.Instance);
        DirectConsume(in ManyNotifyEvent_211.Instance);
        DirectConsume(in ManyNotifyEvent_212.Instance);
        DirectConsume(in ManyNotifyEvent_212.Instance);
        DirectConsume(in ManyNotifyEvent_212.Instance);
        DirectConsume(in ManyNotifyEvent_213.Instance);
        DirectConsume(in ManyNotifyEvent_213.Instance);
        DirectConsume(in ManyNotifyEvent_213.Instance);
        DirectConsume(in ManyNotifyEvent_214.Instance);
        DirectConsume(in ManyNotifyEvent_214.Instance);
        DirectConsume(in ManyNotifyEvent_214.Instance);
        DirectConsume(in ManyNotifyEvent_215.Instance);
        DirectConsume(in ManyNotifyEvent_215.Instance);
        DirectConsume(in ManyNotifyEvent_215.Instance);
        DirectConsume(in ManyNotifyEvent_216.Instance);
        DirectConsume(in ManyNotifyEvent_216.Instance);
        DirectConsume(in ManyNotifyEvent_216.Instance);
        DirectConsume(in ManyNotifyEvent_217.Instance);
        DirectConsume(in ManyNotifyEvent_217.Instance);
        DirectConsume(in ManyNotifyEvent_217.Instance);
        DirectConsume(in ManyNotifyEvent_218.Instance);
        DirectConsume(in ManyNotifyEvent_218.Instance);
        DirectConsume(in ManyNotifyEvent_218.Instance);
        DirectConsume(in ManyNotifyEvent_219.Instance);
        DirectConsume(in ManyNotifyEvent_219.Instance);
        DirectConsume(in ManyNotifyEvent_219.Instance);
        DirectConsume(in ManyNotifyEvent_220.Instance);
        DirectConsume(in ManyNotifyEvent_220.Instance);
        DirectConsume(in ManyNotifyEvent_220.Instance);
        DirectConsume(in ManyNotifyEvent_221.Instance);
        DirectConsume(in ManyNotifyEvent_221.Instance);
        DirectConsume(in ManyNotifyEvent_221.Instance);
        DirectConsume(in ManyNotifyEvent_222.Instance);
        DirectConsume(in ManyNotifyEvent_222.Instance);
        DirectConsume(in ManyNotifyEvent_222.Instance);
        DirectConsume(in ManyNotifyEvent_223.Instance);
        DirectConsume(in ManyNotifyEvent_223.Instance);
        DirectConsume(in ManyNotifyEvent_223.Instance);
        DirectConsume(in ManyNotifyEvent_224.Instance);
        DirectConsume(in ManyNotifyEvent_224.Instance);
        DirectConsume(in ManyNotifyEvent_224.Instance);
        DirectConsume(in ManyNotifyEvent_225.Instance);
        DirectConsume(in ManyNotifyEvent_225.Instance);
        DirectConsume(in ManyNotifyEvent_225.Instance);
        DirectConsume(in ManyNotifyEvent_226.Instance);
        DirectConsume(in ManyNotifyEvent_226.Instance);
        DirectConsume(in ManyNotifyEvent_226.Instance);
        DirectConsume(in ManyNotifyEvent_227.Instance);
        DirectConsume(in ManyNotifyEvent_227.Instance);
        DirectConsume(in ManyNotifyEvent_227.Instance);
        DirectConsume(in ManyNotifyEvent_228.Instance);
        DirectConsume(in ManyNotifyEvent_228.Instance);
        DirectConsume(in ManyNotifyEvent_228.Instance);
        DirectConsume(in ManyNotifyEvent_229.Instance);
        DirectConsume(in ManyNotifyEvent_229.Instance);
        DirectConsume(in ManyNotifyEvent_229.Instance);
        DirectConsume(in ManyNotifyEvent_230.Instance);
        DirectConsume(in ManyNotifyEvent_230.Instance);
        DirectConsume(in ManyNotifyEvent_230.Instance);
        DirectConsume(in ManyNotifyEvent_231.Instance);
        DirectConsume(in ManyNotifyEvent_231.Instance);
        DirectConsume(in ManyNotifyEvent_231.Instance);
        DirectConsume(in ManyNotifyEvent_232.Instance);
        DirectConsume(in ManyNotifyEvent_232.Instance);
        DirectConsume(in ManyNotifyEvent_232.Instance);
        DirectConsume(in ManyNotifyEvent_233.Instance);
        DirectConsume(in ManyNotifyEvent_233.Instance);
        DirectConsume(in ManyNotifyEvent_233.Instance);
        DirectConsume(in ManyNotifyEvent_234.Instance);
        DirectConsume(in ManyNotifyEvent_234.Instance);
        DirectConsume(in ManyNotifyEvent_234.Instance);
        DirectConsume(in ManyNotifyEvent_235.Instance);
        DirectConsume(in ManyNotifyEvent_235.Instance);
        DirectConsume(in ManyNotifyEvent_235.Instance);
        DirectConsume(in ManyNotifyEvent_236.Instance);
        DirectConsume(in ManyNotifyEvent_236.Instance);
        DirectConsume(in ManyNotifyEvent_236.Instance);
        DirectConsume(in ManyNotifyEvent_237.Instance);
        DirectConsume(in ManyNotifyEvent_237.Instance);
        DirectConsume(in ManyNotifyEvent_237.Instance);
        DirectConsume(in ManyNotifyEvent_238.Instance);
        DirectConsume(in ManyNotifyEvent_238.Instance);
        DirectConsume(in ManyNotifyEvent_238.Instance);
        DirectConsume(in ManyNotifyEvent_239.Instance);
        DirectConsume(in ManyNotifyEvent_239.Instance);
        DirectConsume(in ManyNotifyEvent_239.Instance);
        DirectConsume(in ManyNotifyEvent_240.Instance);
        DirectConsume(in ManyNotifyEvent_240.Instance);
        DirectConsume(in ManyNotifyEvent_240.Instance);
        DirectConsume(in ManyNotifyEvent_241.Instance);
        DirectConsume(in ManyNotifyEvent_241.Instance);
        DirectConsume(in ManyNotifyEvent_241.Instance);
        DirectConsume(in ManyNotifyEvent_242.Instance);
        DirectConsume(in ManyNotifyEvent_242.Instance);
        DirectConsume(in ManyNotifyEvent_242.Instance);
        DirectConsume(in ManyNotifyEvent_243.Instance);
        DirectConsume(in ManyNotifyEvent_243.Instance);
        DirectConsume(in ManyNotifyEvent_243.Instance);
        DirectConsume(in ManyNotifyEvent_244.Instance);
        DirectConsume(in ManyNotifyEvent_244.Instance);
        DirectConsume(in ManyNotifyEvent_244.Instance);
        DirectConsume(in ManyNotifyEvent_245.Instance);
        DirectConsume(in ManyNotifyEvent_245.Instance);
        DirectConsume(in ManyNotifyEvent_245.Instance);
        DirectConsume(in ManyNotifyEvent_246.Instance);
        DirectConsume(in ManyNotifyEvent_246.Instance);
        DirectConsume(in ManyNotifyEvent_246.Instance);
        DirectConsume(in ManyNotifyEvent_247.Instance);
        DirectConsume(in ManyNotifyEvent_247.Instance);
        DirectConsume(in ManyNotifyEvent_247.Instance);
        DirectConsume(in ManyNotifyEvent_248.Instance);
        DirectConsume(in ManyNotifyEvent_248.Instance);
        DirectConsume(in ManyNotifyEvent_248.Instance);
        DirectConsume(in ManyNotifyEvent_249.Instance);
        DirectConsume(in ManyNotifyEvent_249.Instance);
        DirectConsume(in ManyNotifyEvent_249.Instance);
        DirectConsume(in ManyNotifyEvent_250.Instance);
        DirectConsume(in ManyNotifyEvent_250.Instance);
        DirectConsume(in ManyNotifyEvent_250.Instance);
        DirectConsume(in ManyNotifyEvent_251.Instance);
        DirectConsume(in ManyNotifyEvent_251.Instance);
        DirectConsume(in ManyNotifyEvent_251.Instance);
        DirectConsume(in ManyNotifyEvent_252.Instance);
        DirectConsume(in ManyNotifyEvent_252.Instance);
        DirectConsume(in ManyNotifyEvent_252.Instance);
        DirectConsume(in ManyNotifyEvent_253.Instance);
        DirectConsume(in ManyNotifyEvent_253.Instance);
        DirectConsume(in ManyNotifyEvent_253.Instance);
        DirectConsume(in ManyNotifyEvent_254.Instance);
        DirectConsume(in ManyNotifyEvent_254.Instance);
        DirectConsume(in ManyNotifyEvent_254.Instance);
        DirectConsume(in ManyNotifyEvent_255.Instance);
        DirectConsume(in ManyNotifyEvent_255.Instance);
        DirectConsume(in ManyNotifyEvent_255.Instance);
    }

    public static void DispatchLayerBase256()
    {
        LayerHub.Send(ManyNotifyEvent_000.Instance);
        LayerHub.Send(ManyNotifyEvent_001.Instance);
        LayerHub.Send(ManyNotifyEvent_002.Instance);
        LayerHub.Send(ManyNotifyEvent_003.Instance);
        LayerHub.Send(ManyNotifyEvent_004.Instance);
        LayerHub.Send(ManyNotifyEvent_005.Instance);
        LayerHub.Send(ManyNotifyEvent_006.Instance);
        LayerHub.Send(ManyNotifyEvent_007.Instance);
        LayerHub.Send(ManyNotifyEvent_008.Instance);
        LayerHub.Send(ManyNotifyEvent_009.Instance);
        LayerHub.Send(ManyNotifyEvent_010.Instance);
        LayerHub.Send(ManyNotifyEvent_011.Instance);
        LayerHub.Send(ManyNotifyEvent_012.Instance);
        LayerHub.Send(ManyNotifyEvent_013.Instance);
        LayerHub.Send(ManyNotifyEvent_014.Instance);
        LayerHub.Send(ManyNotifyEvent_015.Instance);
        LayerHub.Send(ManyNotifyEvent_016.Instance);
        LayerHub.Send(ManyNotifyEvent_017.Instance);
        LayerHub.Send(ManyNotifyEvent_018.Instance);
        LayerHub.Send(ManyNotifyEvent_019.Instance);
        LayerHub.Send(ManyNotifyEvent_020.Instance);
        LayerHub.Send(ManyNotifyEvent_021.Instance);
        LayerHub.Send(ManyNotifyEvent_022.Instance);
        LayerHub.Send(ManyNotifyEvent_023.Instance);
        LayerHub.Send(ManyNotifyEvent_024.Instance);
        LayerHub.Send(ManyNotifyEvent_025.Instance);
        LayerHub.Send(ManyNotifyEvent_026.Instance);
        LayerHub.Send(ManyNotifyEvent_027.Instance);
        LayerHub.Send(ManyNotifyEvent_028.Instance);
        LayerHub.Send(ManyNotifyEvent_029.Instance);
        LayerHub.Send(ManyNotifyEvent_030.Instance);
        LayerHub.Send(ManyNotifyEvent_031.Instance);
        LayerHub.Send(ManyNotifyEvent_032.Instance);
        LayerHub.Send(ManyNotifyEvent_033.Instance);
        LayerHub.Send(ManyNotifyEvent_034.Instance);
        LayerHub.Send(ManyNotifyEvent_035.Instance);
        LayerHub.Send(ManyNotifyEvent_036.Instance);
        LayerHub.Send(ManyNotifyEvent_037.Instance);
        LayerHub.Send(ManyNotifyEvent_038.Instance);
        LayerHub.Send(ManyNotifyEvent_039.Instance);
        LayerHub.Send(ManyNotifyEvent_040.Instance);
        LayerHub.Send(ManyNotifyEvent_041.Instance);
        LayerHub.Send(ManyNotifyEvent_042.Instance);
        LayerHub.Send(ManyNotifyEvent_043.Instance);
        LayerHub.Send(ManyNotifyEvent_044.Instance);
        LayerHub.Send(ManyNotifyEvent_045.Instance);
        LayerHub.Send(ManyNotifyEvent_046.Instance);
        LayerHub.Send(ManyNotifyEvent_047.Instance);
        LayerHub.Send(ManyNotifyEvent_048.Instance);
        LayerHub.Send(ManyNotifyEvent_049.Instance);
        LayerHub.Send(ManyNotifyEvent_050.Instance);
        LayerHub.Send(ManyNotifyEvent_051.Instance);
        LayerHub.Send(ManyNotifyEvent_052.Instance);
        LayerHub.Send(ManyNotifyEvent_053.Instance);
        LayerHub.Send(ManyNotifyEvent_054.Instance);
        LayerHub.Send(ManyNotifyEvent_055.Instance);
        LayerHub.Send(ManyNotifyEvent_056.Instance);
        LayerHub.Send(ManyNotifyEvent_057.Instance);
        LayerHub.Send(ManyNotifyEvent_058.Instance);
        LayerHub.Send(ManyNotifyEvent_059.Instance);
        LayerHub.Send(ManyNotifyEvent_060.Instance);
        LayerHub.Send(ManyNotifyEvent_061.Instance);
        LayerHub.Send(ManyNotifyEvent_062.Instance);
        LayerHub.Send(ManyNotifyEvent_063.Instance);
        LayerHub.Send(ManyNotifyEvent_064.Instance);
        LayerHub.Send(ManyNotifyEvent_065.Instance);
        LayerHub.Send(ManyNotifyEvent_066.Instance);
        LayerHub.Send(ManyNotifyEvent_067.Instance);
        LayerHub.Send(ManyNotifyEvent_068.Instance);
        LayerHub.Send(ManyNotifyEvent_069.Instance);
        LayerHub.Send(ManyNotifyEvent_070.Instance);
        LayerHub.Send(ManyNotifyEvent_071.Instance);
        LayerHub.Send(ManyNotifyEvent_072.Instance);
        LayerHub.Send(ManyNotifyEvent_073.Instance);
        LayerHub.Send(ManyNotifyEvent_074.Instance);
        LayerHub.Send(ManyNotifyEvent_075.Instance);
        LayerHub.Send(ManyNotifyEvent_076.Instance);
        LayerHub.Send(ManyNotifyEvent_077.Instance);
        LayerHub.Send(ManyNotifyEvent_078.Instance);
        LayerHub.Send(ManyNotifyEvent_079.Instance);
        LayerHub.Send(ManyNotifyEvent_080.Instance);
        LayerHub.Send(ManyNotifyEvent_081.Instance);
        LayerHub.Send(ManyNotifyEvent_082.Instance);
        LayerHub.Send(ManyNotifyEvent_083.Instance);
        LayerHub.Send(ManyNotifyEvent_084.Instance);
        LayerHub.Send(ManyNotifyEvent_085.Instance);
        LayerHub.Send(ManyNotifyEvent_086.Instance);
        LayerHub.Send(ManyNotifyEvent_087.Instance);
        LayerHub.Send(ManyNotifyEvent_088.Instance);
        LayerHub.Send(ManyNotifyEvent_089.Instance);
        LayerHub.Send(ManyNotifyEvent_090.Instance);
        LayerHub.Send(ManyNotifyEvent_091.Instance);
        LayerHub.Send(ManyNotifyEvent_092.Instance);
        LayerHub.Send(ManyNotifyEvent_093.Instance);
        LayerHub.Send(ManyNotifyEvent_094.Instance);
        LayerHub.Send(ManyNotifyEvent_095.Instance);
        LayerHub.Send(ManyNotifyEvent_096.Instance);
        LayerHub.Send(ManyNotifyEvent_097.Instance);
        LayerHub.Send(ManyNotifyEvent_098.Instance);
        LayerHub.Send(ManyNotifyEvent_099.Instance);
        LayerHub.Send(ManyNotifyEvent_100.Instance);
        LayerHub.Send(ManyNotifyEvent_101.Instance);
        LayerHub.Send(ManyNotifyEvent_102.Instance);
        LayerHub.Send(ManyNotifyEvent_103.Instance);
        LayerHub.Send(ManyNotifyEvent_104.Instance);
        LayerHub.Send(ManyNotifyEvent_105.Instance);
        LayerHub.Send(ManyNotifyEvent_106.Instance);
        LayerHub.Send(ManyNotifyEvent_107.Instance);
        LayerHub.Send(ManyNotifyEvent_108.Instance);
        LayerHub.Send(ManyNotifyEvent_109.Instance);
        LayerHub.Send(ManyNotifyEvent_110.Instance);
        LayerHub.Send(ManyNotifyEvent_111.Instance);
        LayerHub.Send(ManyNotifyEvent_112.Instance);
        LayerHub.Send(ManyNotifyEvent_113.Instance);
        LayerHub.Send(ManyNotifyEvent_114.Instance);
        LayerHub.Send(ManyNotifyEvent_115.Instance);
        LayerHub.Send(ManyNotifyEvent_116.Instance);
        LayerHub.Send(ManyNotifyEvent_117.Instance);
        LayerHub.Send(ManyNotifyEvent_118.Instance);
        LayerHub.Send(ManyNotifyEvent_119.Instance);
        LayerHub.Send(ManyNotifyEvent_120.Instance);
        LayerHub.Send(ManyNotifyEvent_121.Instance);
        LayerHub.Send(ManyNotifyEvent_122.Instance);
        LayerHub.Send(ManyNotifyEvent_123.Instance);
        LayerHub.Send(ManyNotifyEvent_124.Instance);
        LayerHub.Send(ManyNotifyEvent_125.Instance);
        LayerHub.Send(ManyNotifyEvent_126.Instance);
        LayerHub.Send(ManyNotifyEvent_127.Instance);
        LayerHub.Send(ManyNotifyEvent_128.Instance);
        LayerHub.Send(ManyNotifyEvent_129.Instance);
        LayerHub.Send(ManyNotifyEvent_130.Instance);
        LayerHub.Send(ManyNotifyEvent_131.Instance);
        LayerHub.Send(ManyNotifyEvent_132.Instance);
        LayerHub.Send(ManyNotifyEvent_133.Instance);
        LayerHub.Send(ManyNotifyEvent_134.Instance);
        LayerHub.Send(ManyNotifyEvent_135.Instance);
        LayerHub.Send(ManyNotifyEvent_136.Instance);
        LayerHub.Send(ManyNotifyEvent_137.Instance);
        LayerHub.Send(ManyNotifyEvent_138.Instance);
        LayerHub.Send(ManyNotifyEvent_139.Instance);
        LayerHub.Send(ManyNotifyEvent_140.Instance);
        LayerHub.Send(ManyNotifyEvent_141.Instance);
        LayerHub.Send(ManyNotifyEvent_142.Instance);
        LayerHub.Send(ManyNotifyEvent_143.Instance);
        LayerHub.Send(ManyNotifyEvent_144.Instance);
        LayerHub.Send(ManyNotifyEvent_145.Instance);
        LayerHub.Send(ManyNotifyEvent_146.Instance);
        LayerHub.Send(ManyNotifyEvent_147.Instance);
        LayerHub.Send(ManyNotifyEvent_148.Instance);
        LayerHub.Send(ManyNotifyEvent_149.Instance);
        LayerHub.Send(ManyNotifyEvent_150.Instance);
        LayerHub.Send(ManyNotifyEvent_151.Instance);
        LayerHub.Send(ManyNotifyEvent_152.Instance);
        LayerHub.Send(ManyNotifyEvent_153.Instance);
        LayerHub.Send(ManyNotifyEvent_154.Instance);
        LayerHub.Send(ManyNotifyEvent_155.Instance);
        LayerHub.Send(ManyNotifyEvent_156.Instance);
        LayerHub.Send(ManyNotifyEvent_157.Instance);
        LayerHub.Send(ManyNotifyEvent_158.Instance);
        LayerHub.Send(ManyNotifyEvent_159.Instance);
        LayerHub.Send(ManyNotifyEvent_160.Instance);
        LayerHub.Send(ManyNotifyEvent_161.Instance);
        LayerHub.Send(ManyNotifyEvent_162.Instance);
        LayerHub.Send(ManyNotifyEvent_163.Instance);
        LayerHub.Send(ManyNotifyEvent_164.Instance);
        LayerHub.Send(ManyNotifyEvent_165.Instance);
        LayerHub.Send(ManyNotifyEvent_166.Instance);
        LayerHub.Send(ManyNotifyEvent_167.Instance);
        LayerHub.Send(ManyNotifyEvent_168.Instance);
        LayerHub.Send(ManyNotifyEvent_169.Instance);
        LayerHub.Send(ManyNotifyEvent_170.Instance);
        LayerHub.Send(ManyNotifyEvent_171.Instance);
        LayerHub.Send(ManyNotifyEvent_172.Instance);
        LayerHub.Send(ManyNotifyEvent_173.Instance);
        LayerHub.Send(ManyNotifyEvent_174.Instance);
        LayerHub.Send(ManyNotifyEvent_175.Instance);
        LayerHub.Send(ManyNotifyEvent_176.Instance);
        LayerHub.Send(ManyNotifyEvent_177.Instance);
        LayerHub.Send(ManyNotifyEvent_178.Instance);
        LayerHub.Send(ManyNotifyEvent_179.Instance);
        LayerHub.Send(ManyNotifyEvent_180.Instance);
        LayerHub.Send(ManyNotifyEvent_181.Instance);
        LayerHub.Send(ManyNotifyEvent_182.Instance);
        LayerHub.Send(ManyNotifyEvent_183.Instance);
        LayerHub.Send(ManyNotifyEvent_184.Instance);
        LayerHub.Send(ManyNotifyEvent_185.Instance);
        LayerHub.Send(ManyNotifyEvent_186.Instance);
        LayerHub.Send(ManyNotifyEvent_187.Instance);
        LayerHub.Send(ManyNotifyEvent_188.Instance);
        LayerHub.Send(ManyNotifyEvent_189.Instance);
        LayerHub.Send(ManyNotifyEvent_190.Instance);
        LayerHub.Send(ManyNotifyEvent_191.Instance);
        LayerHub.Send(ManyNotifyEvent_192.Instance);
        LayerHub.Send(ManyNotifyEvent_193.Instance);
        LayerHub.Send(ManyNotifyEvent_194.Instance);
        LayerHub.Send(ManyNotifyEvent_195.Instance);
        LayerHub.Send(ManyNotifyEvent_196.Instance);
        LayerHub.Send(ManyNotifyEvent_197.Instance);
        LayerHub.Send(ManyNotifyEvent_198.Instance);
        LayerHub.Send(ManyNotifyEvent_199.Instance);
        LayerHub.Send(ManyNotifyEvent_200.Instance);
        LayerHub.Send(ManyNotifyEvent_201.Instance);
        LayerHub.Send(ManyNotifyEvent_202.Instance);
        LayerHub.Send(ManyNotifyEvent_203.Instance);
        LayerHub.Send(ManyNotifyEvent_204.Instance);
        LayerHub.Send(ManyNotifyEvent_205.Instance);
        LayerHub.Send(ManyNotifyEvent_206.Instance);
        LayerHub.Send(ManyNotifyEvent_207.Instance);
        LayerHub.Send(ManyNotifyEvent_208.Instance);
        LayerHub.Send(ManyNotifyEvent_209.Instance);
        LayerHub.Send(ManyNotifyEvent_210.Instance);
        LayerHub.Send(ManyNotifyEvent_211.Instance);
        LayerHub.Send(ManyNotifyEvent_212.Instance);
        LayerHub.Send(ManyNotifyEvent_213.Instance);
        LayerHub.Send(ManyNotifyEvent_214.Instance);
        LayerHub.Send(ManyNotifyEvent_215.Instance);
        LayerHub.Send(ManyNotifyEvent_216.Instance);
        LayerHub.Send(ManyNotifyEvent_217.Instance);
        LayerHub.Send(ManyNotifyEvent_218.Instance);
        LayerHub.Send(ManyNotifyEvent_219.Instance);
        LayerHub.Send(ManyNotifyEvent_220.Instance);
        LayerHub.Send(ManyNotifyEvent_221.Instance);
        LayerHub.Send(ManyNotifyEvent_222.Instance);
        LayerHub.Send(ManyNotifyEvent_223.Instance);
        LayerHub.Send(ManyNotifyEvent_224.Instance);
        LayerHub.Send(ManyNotifyEvent_225.Instance);
        LayerHub.Send(ManyNotifyEvent_226.Instance);
        LayerHub.Send(ManyNotifyEvent_227.Instance);
        LayerHub.Send(ManyNotifyEvent_228.Instance);
        LayerHub.Send(ManyNotifyEvent_229.Instance);
        LayerHub.Send(ManyNotifyEvent_230.Instance);
        LayerHub.Send(ManyNotifyEvent_231.Instance);
        LayerHub.Send(ManyNotifyEvent_232.Instance);
        LayerHub.Send(ManyNotifyEvent_233.Instance);
        LayerHub.Send(ManyNotifyEvent_234.Instance);
        LayerHub.Send(ManyNotifyEvent_235.Instance);
        LayerHub.Send(ManyNotifyEvent_236.Instance);
        LayerHub.Send(ManyNotifyEvent_237.Instance);
        LayerHub.Send(ManyNotifyEvent_238.Instance);
        LayerHub.Send(ManyNotifyEvent_239.Instance);
        LayerHub.Send(ManyNotifyEvent_240.Instance);
        LayerHub.Send(ManyNotifyEvent_241.Instance);
        LayerHub.Send(ManyNotifyEvent_242.Instance);
        LayerHub.Send(ManyNotifyEvent_243.Instance);
        LayerHub.Send(ManyNotifyEvent_244.Instance);
        LayerHub.Send(ManyNotifyEvent_245.Instance);
        LayerHub.Send(ManyNotifyEvent_246.Instance);
        LayerHub.Send(ManyNotifyEvent_247.Instance);
        LayerHub.Send(ManyNotifyEvent_248.Instance);
        LayerHub.Send(ManyNotifyEvent_249.Instance);
        LayerHub.Send(ManyNotifyEvent_250.Instance);
        LayerHub.Send(ManyNotifyEvent_251.Instance);
        LayerHub.Send(ManyNotifyEvent_252.Instance);
        LayerHub.Send(ManyNotifyEvent_253.Instance);
        LayerHub.Send(ManyNotifyEvent_254.Instance);
        LayerHub.Send(ManyNotifyEvent_255.Instance);
    }

    public static void DispatchMessagePipe256(ManyNotifyBatch256Publishers publishers)
    {
        publishers.P000.Publish(ManyNotifyEvent_000.Instance);
        publishers.P001.Publish(ManyNotifyEvent_001.Instance);
        publishers.P002.Publish(ManyNotifyEvent_002.Instance);
        publishers.P003.Publish(ManyNotifyEvent_003.Instance);
        publishers.P004.Publish(ManyNotifyEvent_004.Instance);
        publishers.P005.Publish(ManyNotifyEvent_005.Instance);
        publishers.P006.Publish(ManyNotifyEvent_006.Instance);
        publishers.P007.Publish(ManyNotifyEvent_007.Instance);
        publishers.P008.Publish(ManyNotifyEvent_008.Instance);
        publishers.P009.Publish(ManyNotifyEvent_009.Instance);
        publishers.P010.Publish(ManyNotifyEvent_010.Instance);
        publishers.P011.Publish(ManyNotifyEvent_011.Instance);
        publishers.P012.Publish(ManyNotifyEvent_012.Instance);
        publishers.P013.Publish(ManyNotifyEvent_013.Instance);
        publishers.P014.Publish(ManyNotifyEvent_014.Instance);
        publishers.P015.Publish(ManyNotifyEvent_015.Instance);
        publishers.P016.Publish(ManyNotifyEvent_016.Instance);
        publishers.P017.Publish(ManyNotifyEvent_017.Instance);
        publishers.P018.Publish(ManyNotifyEvent_018.Instance);
        publishers.P019.Publish(ManyNotifyEvent_019.Instance);
        publishers.P020.Publish(ManyNotifyEvent_020.Instance);
        publishers.P021.Publish(ManyNotifyEvent_021.Instance);
        publishers.P022.Publish(ManyNotifyEvent_022.Instance);
        publishers.P023.Publish(ManyNotifyEvent_023.Instance);
        publishers.P024.Publish(ManyNotifyEvent_024.Instance);
        publishers.P025.Publish(ManyNotifyEvent_025.Instance);
        publishers.P026.Publish(ManyNotifyEvent_026.Instance);
        publishers.P027.Publish(ManyNotifyEvent_027.Instance);
        publishers.P028.Publish(ManyNotifyEvent_028.Instance);
        publishers.P029.Publish(ManyNotifyEvent_029.Instance);
        publishers.P030.Publish(ManyNotifyEvent_030.Instance);
        publishers.P031.Publish(ManyNotifyEvent_031.Instance);
        publishers.P032.Publish(ManyNotifyEvent_032.Instance);
        publishers.P033.Publish(ManyNotifyEvent_033.Instance);
        publishers.P034.Publish(ManyNotifyEvent_034.Instance);
        publishers.P035.Publish(ManyNotifyEvent_035.Instance);
        publishers.P036.Publish(ManyNotifyEvent_036.Instance);
        publishers.P037.Publish(ManyNotifyEvent_037.Instance);
        publishers.P038.Publish(ManyNotifyEvent_038.Instance);
        publishers.P039.Publish(ManyNotifyEvent_039.Instance);
        publishers.P040.Publish(ManyNotifyEvent_040.Instance);
        publishers.P041.Publish(ManyNotifyEvent_041.Instance);
        publishers.P042.Publish(ManyNotifyEvent_042.Instance);
        publishers.P043.Publish(ManyNotifyEvent_043.Instance);
        publishers.P044.Publish(ManyNotifyEvent_044.Instance);
        publishers.P045.Publish(ManyNotifyEvent_045.Instance);
        publishers.P046.Publish(ManyNotifyEvent_046.Instance);
        publishers.P047.Publish(ManyNotifyEvent_047.Instance);
        publishers.P048.Publish(ManyNotifyEvent_048.Instance);
        publishers.P049.Publish(ManyNotifyEvent_049.Instance);
        publishers.P050.Publish(ManyNotifyEvent_050.Instance);
        publishers.P051.Publish(ManyNotifyEvent_051.Instance);
        publishers.P052.Publish(ManyNotifyEvent_052.Instance);
        publishers.P053.Publish(ManyNotifyEvent_053.Instance);
        publishers.P054.Publish(ManyNotifyEvent_054.Instance);
        publishers.P055.Publish(ManyNotifyEvent_055.Instance);
        publishers.P056.Publish(ManyNotifyEvent_056.Instance);
        publishers.P057.Publish(ManyNotifyEvent_057.Instance);
        publishers.P058.Publish(ManyNotifyEvent_058.Instance);
        publishers.P059.Publish(ManyNotifyEvent_059.Instance);
        publishers.P060.Publish(ManyNotifyEvent_060.Instance);
        publishers.P061.Publish(ManyNotifyEvent_061.Instance);
        publishers.P062.Publish(ManyNotifyEvent_062.Instance);
        publishers.P063.Publish(ManyNotifyEvent_063.Instance);
        publishers.P064.Publish(ManyNotifyEvent_064.Instance);
        publishers.P065.Publish(ManyNotifyEvent_065.Instance);
        publishers.P066.Publish(ManyNotifyEvent_066.Instance);
        publishers.P067.Publish(ManyNotifyEvent_067.Instance);
        publishers.P068.Publish(ManyNotifyEvent_068.Instance);
        publishers.P069.Publish(ManyNotifyEvent_069.Instance);
        publishers.P070.Publish(ManyNotifyEvent_070.Instance);
        publishers.P071.Publish(ManyNotifyEvent_071.Instance);
        publishers.P072.Publish(ManyNotifyEvent_072.Instance);
        publishers.P073.Publish(ManyNotifyEvent_073.Instance);
        publishers.P074.Publish(ManyNotifyEvent_074.Instance);
        publishers.P075.Publish(ManyNotifyEvent_075.Instance);
        publishers.P076.Publish(ManyNotifyEvent_076.Instance);
        publishers.P077.Publish(ManyNotifyEvent_077.Instance);
        publishers.P078.Publish(ManyNotifyEvent_078.Instance);
        publishers.P079.Publish(ManyNotifyEvent_079.Instance);
        publishers.P080.Publish(ManyNotifyEvent_080.Instance);
        publishers.P081.Publish(ManyNotifyEvent_081.Instance);
        publishers.P082.Publish(ManyNotifyEvent_082.Instance);
        publishers.P083.Publish(ManyNotifyEvent_083.Instance);
        publishers.P084.Publish(ManyNotifyEvent_084.Instance);
        publishers.P085.Publish(ManyNotifyEvent_085.Instance);
        publishers.P086.Publish(ManyNotifyEvent_086.Instance);
        publishers.P087.Publish(ManyNotifyEvent_087.Instance);
        publishers.P088.Publish(ManyNotifyEvent_088.Instance);
        publishers.P089.Publish(ManyNotifyEvent_089.Instance);
        publishers.P090.Publish(ManyNotifyEvent_090.Instance);
        publishers.P091.Publish(ManyNotifyEvent_091.Instance);
        publishers.P092.Publish(ManyNotifyEvent_092.Instance);
        publishers.P093.Publish(ManyNotifyEvent_093.Instance);
        publishers.P094.Publish(ManyNotifyEvent_094.Instance);
        publishers.P095.Publish(ManyNotifyEvent_095.Instance);
        publishers.P096.Publish(ManyNotifyEvent_096.Instance);
        publishers.P097.Publish(ManyNotifyEvent_097.Instance);
        publishers.P098.Publish(ManyNotifyEvent_098.Instance);
        publishers.P099.Publish(ManyNotifyEvent_099.Instance);
        publishers.P100.Publish(ManyNotifyEvent_100.Instance);
        publishers.P101.Publish(ManyNotifyEvent_101.Instance);
        publishers.P102.Publish(ManyNotifyEvent_102.Instance);
        publishers.P103.Publish(ManyNotifyEvent_103.Instance);
        publishers.P104.Publish(ManyNotifyEvent_104.Instance);
        publishers.P105.Publish(ManyNotifyEvent_105.Instance);
        publishers.P106.Publish(ManyNotifyEvent_106.Instance);
        publishers.P107.Publish(ManyNotifyEvent_107.Instance);
        publishers.P108.Publish(ManyNotifyEvent_108.Instance);
        publishers.P109.Publish(ManyNotifyEvent_109.Instance);
        publishers.P110.Publish(ManyNotifyEvent_110.Instance);
        publishers.P111.Publish(ManyNotifyEvent_111.Instance);
        publishers.P112.Publish(ManyNotifyEvent_112.Instance);
        publishers.P113.Publish(ManyNotifyEvent_113.Instance);
        publishers.P114.Publish(ManyNotifyEvent_114.Instance);
        publishers.P115.Publish(ManyNotifyEvent_115.Instance);
        publishers.P116.Publish(ManyNotifyEvent_116.Instance);
        publishers.P117.Publish(ManyNotifyEvent_117.Instance);
        publishers.P118.Publish(ManyNotifyEvent_118.Instance);
        publishers.P119.Publish(ManyNotifyEvent_119.Instance);
        publishers.P120.Publish(ManyNotifyEvent_120.Instance);
        publishers.P121.Publish(ManyNotifyEvent_121.Instance);
        publishers.P122.Publish(ManyNotifyEvent_122.Instance);
        publishers.P123.Publish(ManyNotifyEvent_123.Instance);
        publishers.P124.Publish(ManyNotifyEvent_124.Instance);
        publishers.P125.Publish(ManyNotifyEvent_125.Instance);
        publishers.P126.Publish(ManyNotifyEvent_126.Instance);
        publishers.P127.Publish(ManyNotifyEvent_127.Instance);
        publishers.P128.Publish(ManyNotifyEvent_128.Instance);
        publishers.P129.Publish(ManyNotifyEvent_129.Instance);
        publishers.P130.Publish(ManyNotifyEvent_130.Instance);
        publishers.P131.Publish(ManyNotifyEvent_131.Instance);
        publishers.P132.Publish(ManyNotifyEvent_132.Instance);
        publishers.P133.Publish(ManyNotifyEvent_133.Instance);
        publishers.P134.Publish(ManyNotifyEvent_134.Instance);
        publishers.P135.Publish(ManyNotifyEvent_135.Instance);
        publishers.P136.Publish(ManyNotifyEvent_136.Instance);
        publishers.P137.Publish(ManyNotifyEvent_137.Instance);
        publishers.P138.Publish(ManyNotifyEvent_138.Instance);
        publishers.P139.Publish(ManyNotifyEvent_139.Instance);
        publishers.P140.Publish(ManyNotifyEvent_140.Instance);
        publishers.P141.Publish(ManyNotifyEvent_141.Instance);
        publishers.P142.Publish(ManyNotifyEvent_142.Instance);
        publishers.P143.Publish(ManyNotifyEvent_143.Instance);
        publishers.P144.Publish(ManyNotifyEvent_144.Instance);
        publishers.P145.Publish(ManyNotifyEvent_145.Instance);
        publishers.P146.Publish(ManyNotifyEvent_146.Instance);
        publishers.P147.Publish(ManyNotifyEvent_147.Instance);
        publishers.P148.Publish(ManyNotifyEvent_148.Instance);
        publishers.P149.Publish(ManyNotifyEvent_149.Instance);
        publishers.P150.Publish(ManyNotifyEvent_150.Instance);
        publishers.P151.Publish(ManyNotifyEvent_151.Instance);
        publishers.P152.Publish(ManyNotifyEvent_152.Instance);
        publishers.P153.Publish(ManyNotifyEvent_153.Instance);
        publishers.P154.Publish(ManyNotifyEvent_154.Instance);
        publishers.P155.Publish(ManyNotifyEvent_155.Instance);
        publishers.P156.Publish(ManyNotifyEvent_156.Instance);
        publishers.P157.Publish(ManyNotifyEvent_157.Instance);
        publishers.P158.Publish(ManyNotifyEvent_158.Instance);
        publishers.P159.Publish(ManyNotifyEvent_159.Instance);
        publishers.P160.Publish(ManyNotifyEvent_160.Instance);
        publishers.P161.Publish(ManyNotifyEvent_161.Instance);
        publishers.P162.Publish(ManyNotifyEvent_162.Instance);
        publishers.P163.Publish(ManyNotifyEvent_163.Instance);
        publishers.P164.Publish(ManyNotifyEvent_164.Instance);
        publishers.P165.Publish(ManyNotifyEvent_165.Instance);
        publishers.P166.Publish(ManyNotifyEvent_166.Instance);
        publishers.P167.Publish(ManyNotifyEvent_167.Instance);
        publishers.P168.Publish(ManyNotifyEvent_168.Instance);
        publishers.P169.Publish(ManyNotifyEvent_169.Instance);
        publishers.P170.Publish(ManyNotifyEvent_170.Instance);
        publishers.P171.Publish(ManyNotifyEvent_171.Instance);
        publishers.P172.Publish(ManyNotifyEvent_172.Instance);
        publishers.P173.Publish(ManyNotifyEvent_173.Instance);
        publishers.P174.Publish(ManyNotifyEvent_174.Instance);
        publishers.P175.Publish(ManyNotifyEvent_175.Instance);
        publishers.P176.Publish(ManyNotifyEvent_176.Instance);
        publishers.P177.Publish(ManyNotifyEvent_177.Instance);
        publishers.P178.Publish(ManyNotifyEvent_178.Instance);
        publishers.P179.Publish(ManyNotifyEvent_179.Instance);
        publishers.P180.Publish(ManyNotifyEvent_180.Instance);
        publishers.P181.Publish(ManyNotifyEvent_181.Instance);
        publishers.P182.Publish(ManyNotifyEvent_182.Instance);
        publishers.P183.Publish(ManyNotifyEvent_183.Instance);
        publishers.P184.Publish(ManyNotifyEvent_184.Instance);
        publishers.P185.Publish(ManyNotifyEvent_185.Instance);
        publishers.P186.Publish(ManyNotifyEvent_186.Instance);
        publishers.P187.Publish(ManyNotifyEvent_187.Instance);
        publishers.P188.Publish(ManyNotifyEvent_188.Instance);
        publishers.P189.Publish(ManyNotifyEvent_189.Instance);
        publishers.P190.Publish(ManyNotifyEvent_190.Instance);
        publishers.P191.Publish(ManyNotifyEvent_191.Instance);
        publishers.P192.Publish(ManyNotifyEvent_192.Instance);
        publishers.P193.Publish(ManyNotifyEvent_193.Instance);
        publishers.P194.Publish(ManyNotifyEvent_194.Instance);
        publishers.P195.Publish(ManyNotifyEvent_195.Instance);
        publishers.P196.Publish(ManyNotifyEvent_196.Instance);
        publishers.P197.Publish(ManyNotifyEvent_197.Instance);
        publishers.P198.Publish(ManyNotifyEvent_198.Instance);
        publishers.P199.Publish(ManyNotifyEvent_199.Instance);
        publishers.P200.Publish(ManyNotifyEvent_200.Instance);
        publishers.P201.Publish(ManyNotifyEvent_201.Instance);
        publishers.P202.Publish(ManyNotifyEvent_202.Instance);
        publishers.P203.Publish(ManyNotifyEvent_203.Instance);
        publishers.P204.Publish(ManyNotifyEvent_204.Instance);
        publishers.P205.Publish(ManyNotifyEvent_205.Instance);
        publishers.P206.Publish(ManyNotifyEvent_206.Instance);
        publishers.P207.Publish(ManyNotifyEvent_207.Instance);
        publishers.P208.Publish(ManyNotifyEvent_208.Instance);
        publishers.P209.Publish(ManyNotifyEvent_209.Instance);
        publishers.P210.Publish(ManyNotifyEvent_210.Instance);
        publishers.P211.Publish(ManyNotifyEvent_211.Instance);
        publishers.P212.Publish(ManyNotifyEvent_212.Instance);
        publishers.P213.Publish(ManyNotifyEvent_213.Instance);
        publishers.P214.Publish(ManyNotifyEvent_214.Instance);
        publishers.P215.Publish(ManyNotifyEvent_215.Instance);
        publishers.P216.Publish(ManyNotifyEvent_216.Instance);
        publishers.P217.Publish(ManyNotifyEvent_217.Instance);
        publishers.P218.Publish(ManyNotifyEvent_218.Instance);
        publishers.P219.Publish(ManyNotifyEvent_219.Instance);
        publishers.P220.Publish(ManyNotifyEvent_220.Instance);
        publishers.P221.Publish(ManyNotifyEvent_221.Instance);
        publishers.P222.Publish(ManyNotifyEvent_222.Instance);
        publishers.P223.Publish(ManyNotifyEvent_223.Instance);
        publishers.P224.Publish(ManyNotifyEvent_224.Instance);
        publishers.P225.Publish(ManyNotifyEvent_225.Instance);
        publishers.P226.Publish(ManyNotifyEvent_226.Instance);
        publishers.P227.Publish(ManyNotifyEvent_227.Instance);
        publishers.P228.Publish(ManyNotifyEvent_228.Instance);
        publishers.P229.Publish(ManyNotifyEvent_229.Instance);
        publishers.P230.Publish(ManyNotifyEvent_230.Instance);
        publishers.P231.Publish(ManyNotifyEvent_231.Instance);
        publishers.P232.Publish(ManyNotifyEvent_232.Instance);
        publishers.P233.Publish(ManyNotifyEvent_233.Instance);
        publishers.P234.Publish(ManyNotifyEvent_234.Instance);
        publishers.P235.Publish(ManyNotifyEvent_235.Instance);
        publishers.P236.Publish(ManyNotifyEvent_236.Instance);
        publishers.P237.Publish(ManyNotifyEvent_237.Instance);
        publishers.P238.Publish(ManyNotifyEvent_238.Instance);
        publishers.P239.Publish(ManyNotifyEvent_239.Instance);
        publishers.P240.Publish(ManyNotifyEvent_240.Instance);
        publishers.P241.Publish(ManyNotifyEvent_241.Instance);
        publishers.P242.Publish(ManyNotifyEvent_242.Instance);
        publishers.P243.Publish(ManyNotifyEvent_243.Instance);
        publishers.P244.Publish(ManyNotifyEvent_244.Instance);
        publishers.P245.Publish(ManyNotifyEvent_245.Instance);
        publishers.P246.Publish(ManyNotifyEvent_246.Instance);
        publishers.P247.Publish(ManyNotifyEvent_247.Instance);
        publishers.P248.Publish(ManyNotifyEvent_248.Instance);
        publishers.P249.Publish(ManyNotifyEvent_249.Instance);
        publishers.P250.Publish(ManyNotifyEvent_250.Instance);
        publishers.P251.Publish(ManyNotifyEvent_251.Instance);
        publishers.P252.Publish(ManyNotifyEvent_252.Instance);
        publishers.P253.Publish(ManyNotifyEvent_253.Instance);
        publishers.P254.Publish(ManyNotifyEvent_254.Instance);
        publishers.P255.Publish(ManyNotifyEvent_255.Instance);
    }
}

public partial class ManyNotifyManager_000 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_000 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_001 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_001 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_002 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_002 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_003 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_003 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_004 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_004 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_005 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_005 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_006 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_006 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_007 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_007 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_008 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_008 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_009 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_009 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_010 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_010 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_011 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_011 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_012 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_012 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_013 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_013 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_014 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_014 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_015 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_015 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_016 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_016 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_017 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_017 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_018 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_018 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_019 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_019 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_020 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_020 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_021 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_021 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_022 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_022 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_023 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_023 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_024 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_024 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_025 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_025 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_026 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_026 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_027 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_027 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_028 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_028 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_029 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_029 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_030 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_030 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_031 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_031 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_032 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_032 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_033 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_033 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_034 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_034 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_035 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_035 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_036 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_036 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_037 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_037 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_038 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_038 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_039 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_039 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_040 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_040 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_041 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_041 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_042 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_042 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_043 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_043 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_044 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_044 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_045 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_045 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_046 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_046 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_047 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_047 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_048 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_048 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_049 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_049 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_050 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_050 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_051 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_051 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_052 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_052 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_053 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_053 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_054 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_054 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_055 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_055 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_056 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_056 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_057 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_057 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_058 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_058 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_059 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_059 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_060 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_060 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_061 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_061 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_062 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_062 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_063 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_063 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_064 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_064 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_065 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_065 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_066 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_066 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_067 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_067 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_068 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_068 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_069 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_069 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_070 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_070 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_071 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_071 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_072 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_072 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_073 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_073 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_074 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_074 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_075 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_075 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_076 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_076 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_077 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_077 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_078 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_078 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_079 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_079 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_080 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_080 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_081 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_081 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_082 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_082 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_083 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_083 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_084 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_084 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_085 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_085 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_086 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_086 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_087 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_087 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_088 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_088 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_089 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_089 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_090 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_090 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_091 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_091 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_092 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_092 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_093 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_093 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_094 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_094 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_095 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_095 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_096 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_096 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_097 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_097 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_098 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_098 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_099 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_099 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_100 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_100 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_101 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_101 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_102 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_102 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_103 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_103 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_104 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_104 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_105 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_105 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_106 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_106 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_107 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_107 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_108 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_108 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_109 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_109 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_110 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_110 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_111 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_111 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_112 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_112 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_113 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_113 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_114 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_114 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_115 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_115 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_116 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_116 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_117 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_117 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_118 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_118 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_119 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_119 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_120 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_120 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_121 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_121 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_122 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_122 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_123 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_123 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_124 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_124 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_125 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_125 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_126 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_126 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_127 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_127 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_128 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_128 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_129 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_129 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_130 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_130 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_131 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_131 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_132 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_132 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_133 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_133 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_134 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_134 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_135 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_135 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_136 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_136 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_137 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_137 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_138 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_138 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_139 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_139 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_140 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_140 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_141 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_141 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_142 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_142 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_143 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_143 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_144 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_144 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_145 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_145 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_146 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_146 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_147 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_147 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_148 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_148 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_149 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_149 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_150 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_150 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_151 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_151 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_152 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_152 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_153 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_153 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_154 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_154 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_155 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_155 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_156 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_156 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_157 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_157 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_158 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_158 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_159 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_159 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_160 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_160 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_161 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_161 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_162 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_162 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_163 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_163 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_164 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_164 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_165 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_165 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_166 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_166 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_167 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_167 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_168 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_168 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_169 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_169 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_170 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_170 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_171 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_171 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_172 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_172 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_173 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_173 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_174 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_174 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_175 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_175 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_176 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_176 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_177 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_177 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_178 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_178 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_179 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_179 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_180 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_180 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_181 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_181 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_182 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_182 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_183 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_183 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_184 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_184 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_185 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_185 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_186 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_186 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_187 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_187 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_188 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_188 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_189 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_189 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_190 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_190 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_191 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_191 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_192 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_192 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_193 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_193 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_194 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_194 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_195 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_195 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_196 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_196 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_197 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_197 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_198 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_198 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_199 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_199 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_200 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_200 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_201 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_201 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_202 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_202 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_203 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_203 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_204 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_204 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_205 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_205 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_206 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_206 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_207 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_207 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_208 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_208 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_209 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_209 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_210 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_210 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_211 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_211 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_212 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_212 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_213 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_213 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_214 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_214 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_215 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_215 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_216 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_216 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_217 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_217 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_218 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_218 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_219 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_219 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_220 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_220 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_221 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_221 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_222 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_222 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_223 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_223 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_224 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_224 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_225 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_225 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_226 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_226 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_227 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_227 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_228 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_228 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_229 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_229 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_230 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_230 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_231 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_231 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_232 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_232 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_233 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_233 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_234 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_234 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_235 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_235 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_236 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_236 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_237 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_237 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_238 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_238 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_239 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_239 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_240 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_240 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_241 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_241 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_242 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_242 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_243 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_243 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_244 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_244 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_245 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_245 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_246 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_246 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_247 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_247 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_248 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_248 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_249 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_249 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_250 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_250 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_251 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_251 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_252 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_252 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_253 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_253 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_254 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_254 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}

public partial class ManyNotifyManager_255 : IService
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(this);
    }

    [SubscribeNotify]
    public void OnNotify(in ManyNotifyEvent_255 payload)
    {
        Volatile.Write(ref CompareSink.IntValue, payload.Value);
    }
}