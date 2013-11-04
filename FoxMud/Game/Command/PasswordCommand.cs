using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command
{
    [Command("password", false, TickDelay.Instant)]
    class PasswordCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: password <old> <new> <new>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentString) || context.Arguments.Count != 3)
            {
                PrintSyntax(session);
                return;
            }

            var oldPassword = context.Arguments[0];
            var newPassword = context.Arguments[1];
            var dupePassword = context.Arguments[2];

            if (!session.Player.CheckPassword(oldPassword))
            {
                session.WriteLine("Invalid password.");
                return;
            }

            if (newPassword != dupePassword)
            {
                session.WriteLine("New paswords don't match.");
                return;
            }

            session.Player.PasswordHash = newPassword;
            Server.Current.Database.Save(session.Player);
            session.WriteLine("Your password has been changed.");
        }
    }
}
