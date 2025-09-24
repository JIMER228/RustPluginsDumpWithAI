
namespace Oxide.Plugins
{
    [Info("PaidConnection", "value", "1.0.0")]
    [Description("PaidConnection")]
    public class PaidConnection : RustPlugin
    {
        private void OnServerInitialized()
        {
            permission.RegisterPermission("paidconnection.allow", this);
        }

        private object CanClientLogin(Network.Connection connection)
        {
            if(connection.os == "editor" && permission.UserHasPermission(connection.userid.ToString(), "paidconnection.allow") == false)
            {
                return "Купите вход на сервер: TRASHRUST.RU";
            }

            return null;
        }
    }
}
