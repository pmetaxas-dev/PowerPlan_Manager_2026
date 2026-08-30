using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Power_Plan_Manager_Take_8;

internal enum PowerPlanPersonality
{
    Unknown,
    MaximumPowerSavings,
    Balanced,
    MaximumPerformance
}

internal sealed record PowerPlanInfo(
    Guid Id,
    string Name,
    uint? MaxProcessorStateAc,
    uint? MaxProcessorStateDc,
    PowerPlanPersonality Personality = PowerPlanPersonality.Unknown);

internal interface IPowerPlanService
{
    IReadOnlyList<PowerPlanInfo> GetPlans();
    Guid? GetActivePlan();
    uint SetActivePlan(Guid planId);
    uint DuplicatePlan(Guid sourcePlanId, out Guid newPlanId);
    uint DeletePlan(Guid planId);
    uint SetMaxProcessorState(Guid planId, uint value);
    uint SetFriendlyName(Guid planId, string name);
    bool CanCreateScheme();
    bool CanWriteProcessorState();
}

internal interface IPowerPlanStateStore
{
    string IdleThrottleGuid { get; set; }
    string NormalPlanGuid { get; set; }
    int LegacyCleanupVersion { get; set; }
    void Save();
}

internal sealed class SettingsPowerPlanStateStore : IPowerPlanStateStore
{
    public string IdleThrottleGuid
    {
        get => Properties.Settings.Default.IdleThrottleGuid;
        set => Properties.Settings.Default.IdleThrottleGuid = value;
    }

    public string NormalPlanGuid
    {
        get => Properties.Settings.Default.NormalPlanGuid;
        set => Properties.Settings.Default.NormalPlanGuid = value;
    }

    public int LegacyCleanupVersion
    {
        get => Properties.Settings.Default.LegacyCleanupVersion;
        set => Properties.Settings.Default.LegacyCleanupVersion = value;
    }

    public void Save()
    {
        Properties.Settings.Default.Save();
    }
}

internal sealed class WindowsPowerPlanService : IPowerPlanService
{
    private const uint ErrorSuccess = 0;
    private const uint ErrorMoreData = 234;
    private const uint ErrorNoMoreItems = 259;

    private const uint AccessAcPowerSettingIndex = 0;
    private const uint AccessDcPowerSettingIndex = 1;
    private const uint AccessScheme = 16;
    private const uint AccessCreateScheme = 20;

    private static readonly Guid ProcessorSubgroup =
        new("54533251-82be-4824-96c1-47b60b740d00");

    private static readonly Guid MaximumProcessorState =
        new("bc5038f7-23e0-4960-96da-33abaf5935ec");

    private static readonly Guid PowerSchemePersonality =
        new("245d8541-3943-4422-b025-13a784f679b7");

    [DllImport("powrprof.dll")]
    private static extern uint PowerEnumerate(
        IntPtr rootPowerKey,
        IntPtr schemeGuid,
        IntPtr subgroupGuid,
        uint accessFlags,
        uint index,
        [Out] byte[] buffer,
        ref uint bufferSize);

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
    private static extern uint PowerReadFriendlyName(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        IntPtr subgroupGuid,
        IntPtr powerSettingGuid,
        [Out] byte[]? buffer,
        ref uint bufferSize);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadACValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subgroupGuid,
        ref Guid powerSettingGuid,
        out uint valueIndex);

    [DllImport("powrprof.dll", EntryPoint = "PowerReadACValueIndex")]
    private static extern uint PowerReadACValueIndexWithoutSubgroup(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        IntPtr subgroupGuid,
        ref Guid powerSettingGuid,
        out uint valueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerReadDCValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subgroupGuid,
        ref Guid powerSettingGuid,
        out uint valueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerWriteACValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subgroupGuid,
        ref Guid powerSettingGuid,
        uint valueIndex);

    [DllImport("powrprof.dll")]
    private static extern uint PowerWriteDCValueIndex(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        ref Guid subgroupGuid,
        ref Guid powerSettingGuid,
        uint valueIndex);

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
    private static extern uint PowerWriteFriendlyName(
        IntPtr rootPowerKey,
        ref Guid schemeGuid,
        IntPtr subgroupGuid,
        IntPtr powerSettingGuid,
        [MarshalAs(UnmanagedType.LPWStr)] string buffer,
        uint bufferSize);

    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(
        IntPtr userRootPowerKey,
        out IntPtr activePolicyGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerSetActiveScheme(
        IntPtr userRootPowerKey,
        ref Guid schemeGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerDuplicateScheme(
        IntPtr rootPowerKey,
        ref Guid sourceSchemeGuid,
        out IntPtr destinationSchemeGuid);

    [DllImport("powrprof.dll")]
    private static extern uint PowerDeleteScheme(
        IntPtr rootPowerKey,
        ref Guid schemeGuid);

    [DllImport("powrprof.dll", EntryPoint = "PowerSettingAccessCheck")]
    private static extern uint PowerSettingAccessCheckWithoutGuid(
        uint accessFlags,
        IntPtr powerGuid);

    [DllImport("powrprof.dll", EntryPoint = "PowerSettingAccessCheck")]
    private static extern uint PowerSettingAccessCheckWithGuid(
        uint accessFlags,
        ref Guid powerGuid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    public IReadOnlyList<PowerPlanInfo> GetPlans()
    {
        var plans = new List<PowerPlanInfo>();

        for (uint index = 0; ; index++)
        {
            uint bufferSize = 16;
            byte[] buffer = new byte[bufferSize];
            uint result = PowerEnumerate(
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                AccessScheme,
                index,
                buffer,
                ref bufferSize);

            if (result == ErrorNoMoreItems)
            {
                break;
            }

            if (result == ErrorMoreData)
            {
                buffer = new byte[bufferSize];
                result = PowerEnumerate(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    AccessScheme,
                    index,
                    buffer,
                    ref bufferSize);
            }

            if (result != ErrorSuccess || bufferSize < 16)
            {
                throw new Win32Exception((int)result, "Unable to enumerate Windows power schemes.");
            }

            byte[] guidBytes = new byte[16];
            Array.Copy(buffer, guidBytes, 16);
            Guid planId = new(guidBytes);

            plans.Add(new PowerPlanInfo(
                planId,
                ReadFriendlyName(planId),
                ReadMaxProcessorState(planId, acPower: true),
                ReadMaxProcessorState(planId, acPower: false),
                ReadPersonality(planId)));
        }

        return plans;
    }

    public Guid? GetActivePlan()
    {
        uint result = PowerGetActiveScheme(IntPtr.Zero, out IntPtr guidPointer);
        if (result != ErrorSuccess || guidPointer == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStructure<Guid>(guidPointer);
        }
        finally
        {
            LocalFree(guidPointer);
        }
    }

    public uint SetActivePlan(Guid planId)
    {
        return PowerSetActiveScheme(IntPtr.Zero, ref planId);
    }

    public uint DuplicatePlan(Guid sourcePlanId, out Guid newPlanId)
    {
        newPlanId = Guid.Empty;
        uint result = PowerDuplicateScheme(
            IntPtr.Zero,
            ref sourcePlanId,
            out IntPtr guidPointer);

        if (result != ErrorSuccess || guidPointer == IntPtr.Zero)
        {
            return result;
        }

        try
        {
            newPlanId = Marshal.PtrToStructure<Guid>(guidPointer);
            return result;
        }
        finally
        {
            LocalFree(guidPointer);
        }
    }

    public uint DeletePlan(Guid planId)
    {
        return PowerDeleteScheme(IntPtr.Zero, ref planId);
    }

    public uint SetMaxProcessorState(Guid planId, uint value)
    {
        Guid subgroup = ProcessorSubgroup;
        Guid setting = MaximumProcessorState;

        uint result = PowerWriteACValueIndex(
            IntPtr.Zero,
            ref planId,
            ref subgroup,
            ref setting,
            value);

        if (result != ErrorSuccess)
        {
            return result;
        }

        subgroup = ProcessorSubgroup;
        setting = MaximumProcessorState;
        return PowerWriteDCValueIndex(
            IntPtr.Zero,
            ref planId,
            ref subgroup,
            ref setting,
            value);
    }

    public uint SetFriendlyName(Guid planId, string name)
    {
        uint bufferSize = checked((uint)((name.Length + 1) * sizeof(char)));
        return PowerWriteFriendlyName(
            IntPtr.Zero,
            ref planId,
            IntPtr.Zero,
            IntPtr.Zero,
            name,
            bufferSize);
    }

    public bool CanCreateScheme()
    {
        return PowerSettingAccessCheckWithoutGuid(AccessCreateScheme, IntPtr.Zero) == ErrorSuccess;
    }

    public bool CanWriteProcessorState()
    {
        Guid setting = MaximumProcessorState;
        uint acResult = PowerSettingAccessCheckWithGuid(AccessAcPowerSettingIndex, ref setting);

        setting = MaximumProcessorState;
        uint dcResult = PowerSettingAccessCheckWithGuid(AccessDcPowerSettingIndex, ref setting);

        return acResult == ErrorSuccess && dcResult == ErrorSuccess;
    }

    private static string ReadFriendlyName(Guid planId)
    {
        uint bufferSize = 0;
        uint result = PowerReadFriendlyName(
            IntPtr.Zero,
            ref planId,
            IntPtr.Zero,
            IntPtr.Zero,
            null,
            ref bufferSize);

        if ((result != ErrorSuccess && result != ErrorMoreData) || bufferSize == 0)
        {
            return string.Empty;
        }

        byte[] buffer = new byte[bufferSize];
        result = PowerReadFriendlyName(
            IntPtr.Zero,
            ref planId,
            IntPtr.Zero,
            IntPtr.Zero,
            buffer,
            ref bufferSize);

        if (result != ErrorSuccess)
        {
            return string.Empty;
        }

        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    private static uint? ReadMaxProcessorState(Guid planId, bool acPower)
    {
        Guid subgroup = ProcessorSubgroup;
        Guid setting = MaximumProcessorState;
        uint result = acPower
            ? PowerReadACValueIndex(
                IntPtr.Zero,
                ref planId,
                ref subgroup,
                ref setting,
                out uint value)
            : PowerReadDCValueIndex(
                IntPtr.Zero,
                ref planId,
                ref subgroup,
                ref setting,
                out value);

        return result == ErrorSuccess ? value : null;
    }

    private static PowerPlanPersonality ReadPersonality(Guid planId)
    {
        Guid setting = PowerSchemePersonality;
        uint result = PowerReadACValueIndexWithoutSubgroup(
            IntPtr.Zero,
            ref planId,
            IntPtr.Zero,
            ref setting,
            out uint personalityIndex);

        if (result != ErrorSuccess)
        {
            return PowerPlanPersonality.Unknown;
        }

        return personalityIndex switch
        {
            0 => PowerPlanPersonality.MaximumPowerSavings,
            1 => PowerPlanPersonality.MaximumPerformance,
            2 => PowerPlanPersonality.Balanced,
            _ => PowerPlanPersonality.Unknown
        };
    }
}
