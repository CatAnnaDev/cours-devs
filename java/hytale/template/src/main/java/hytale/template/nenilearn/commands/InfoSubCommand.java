package hytale.template.nenilearn.commands;

import com.hypixel.hytale.server.core.Message;
import com.hypixel.hytale.server.core.command.system.CommandContext;
import com.hypixel.hytale.server.core.command.system.basecommands.CommandBase;

import hytale.template.nenilearn.NeniLearnPlugin;

import javax.annotation.Nonnull;

/**
 * /neni info - Show plugin information
 */
public class InfoSubCommand extends CommandBase {

    public InfoSubCommand() {
        super("info", "Show plugin information");
        this.setPermissionGroup(null);
    }

    @Override
    protected boolean canGeneratePermission() {
        return false;
    }

    @Override
    protected void executeSync(@Nonnull CommandContext context) {
        NeniLearnPlugin plugin = NeniLearnPlugin.getInstance();

        context.sendMessage(Message.raw(""));
        context.sendMessage(Message.raw("=== NeniLearn Info ==="));
        context.sendMessage(Message.raw("Name: NeniLearn"));
        context.sendMessage(Message.raw("Version: 1.0.0"));
        context.sendMessage(Message.raw("Author: anna"));
        context.sendMessage(Message.raw("Status: " + (plugin != null ? "Running" : "Not loaded")));
        context.sendMessage(Message.raw("===================="));
    }
}