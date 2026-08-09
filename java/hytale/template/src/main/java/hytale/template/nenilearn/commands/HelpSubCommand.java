package hytale.template.nenilearn.commands;

import com.hypixel.hytale.server.core.Message;
import com.hypixel.hytale.server.core.command.system.CommandContext;
import com.hypixel.hytale.server.core.command.system.basecommands.CommandBase;

import javax.annotation.Nonnull;

/**
 * /neni help - Show available commands
 */
public class HelpSubCommand extends CommandBase {

    public HelpSubCommand() {
        super("help", "Show available commands");
        this.setPermissionGroup(null);
    }

    @Override
    protected boolean canGeneratePermission() {
        return false;
    }

    @Override
    protected void executeSync(@Nonnull CommandContext context) {
        context.sendMessage(Message.raw(""));
        context.sendMessage(Message.raw("=== NeniLearn Commands ==="));
        context.sendMessage(Message.raw("/neni help - Show this help message"));
        context.sendMessage(Message.raw("/neni info - Show plugin information"));
        context.sendMessage(Message.raw("/neni reload - Reload configuration"));
        context.sendMessage(Message.raw("/neni ui - Open the dashboard UI"));
        context.sendMessage(Message.raw("========================"));
    }
}