using System.Collections.Generic;

namespace Oxide.Plugins
{
    //Define:FileOrder=3
    public partial class GatherRewards
    {
        private string Lang(string key, string userId = null) =>
            lang.GetMessage(key, this, userId);

        private void Language()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "ReceivedForGather", "You have received ${0} for gathering {1}." },
                { "LostForGather", "You have lost ${0} for gathering {1}." },
                { "ReceivedForKill", "You have received ${0} for killing a {1}." },
                { "LostForKill", "You have lost ${0} for killing a {1}." },
                { "NoPermission", "You have no permission to use this command." },
                { "Usage", "Usage: /{0} [value] [amount]" },
                { "NotaNumber", "Error: value is not a number." },
                { "Success", "Successfully changed '{0}' to earn amount '{1}'." },
                { "ValueDoesNotExist", "Value '{0}' does not exist." }
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "ReceivedForGather", "Вы получили ${0} за сбор {1}." },
                { "LostForGather", "Вы потеряли ${0} за сбор {1}." },
                { "ReceivedForKill", "Вы получили ${0} за убийство {1}." },
                { "LostForKill", "Вы потеряли $ {0} за убийство {1}." },
                { "NoPermission", "У вас нет прав использовать эту команду." },
                { "Usage", "Использование: / {0} [значение] [количество]" },
                { "NotaNumber", "Ошибка: значение не является числом." },
                { "Success", "Успешно изменено '{0}', чтобы заработать деньги '{1}'." },
                { "ValueDoesNotExist", "Значение '{0}' не существует." }
            }, this, "ru");
        }

    }
}