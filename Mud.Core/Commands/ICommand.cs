namespace Mud.Core.Commands
{
    using System.Collections.Generic;

    public interface ICommand
    {
        string Name { get; }

        IReadOnlyList<string> Aliases { get; }

        string Description { get; }

        void Execute(CommandContext context);
    }
}
