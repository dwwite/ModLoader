namespace NeoModLoader.General.Event
{
    public abstract class AbstractHandler<TEventArgs>
    {
    }

    public abstract class BaseListener
    {
    }

    public abstract class AbstractListener<THandler, TEventArgs> : BaseListener
        where THandler : class
    {
    }

    public static class ListenerManager
    {
    }
}

namespace NeoModLoader.General.Event.Handlers
{
    public sealed class ActorTryToAttackHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class AllianceCreateHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class CityCreateHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class ClanCreateHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class CultureCreateHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class KingdomSetupHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class PlotStartHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class WarEndHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class WarStartHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }

    public sealed class WorldLogMessageHandler : NeoModLoader.General.Event.AbstractHandler<object>
    {
    }
}

namespace NeoModLoader.General.Event.Listeners
{
    public sealed class ActorTryToAttackListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.ActorTryToAttackHandler, object>
    {
    }

    public sealed class AllianceCreateListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.AllianceCreateHandler, object>
    {
    }

    public sealed class CityCreateListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.CityCreateHandler, object>
    {
    }

    public sealed class ClanCreateListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.ClanCreateHandler, object>
    {
    }

    public sealed class CultureCreateListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.CultureCreateHandler, object>
    {
    }

    public sealed class KingdomSetupListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.KingdomSetupHandler, object>
    {
    }

    public sealed class PlotStartListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.PlotStartHandler, object>
    {
    }

    public sealed class WarEndListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.WarEndHandler, object>
    {
    }

    public sealed class WarStartListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.WarStartHandler, object>
    {
    }

    public sealed class WorldLogMessageListener : NeoModLoader.General.Event.AbstractListener<NeoModLoader.General.Event.Handlers.WorldLogMessageHandler, object>
    {
    }
}
