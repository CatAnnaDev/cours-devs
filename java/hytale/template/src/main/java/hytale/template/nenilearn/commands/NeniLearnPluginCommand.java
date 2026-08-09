package hytale.template.nenilearn.commands;

import com.hypixel.hytale.server.core.command.system.basecommands.AbstractCommandCollection;

/**
 * Main command for NeniLearn plugin.
 *
 * Usage:
 * - /neni help - Show available commands
 * - /neni info - Show plugin information
 * - /neni reload - Reload plugin configuration
 * - /neni ui - Open the plugin dashboard
 */
public class NeniLearnPluginCommand extends AbstractCommandCollection {

    public NeniLearnPluginCommand() {
        super("neni", "NeniLearn plugin commands");

        // Add subcommands
        this.addSubCommand(new HelpSubCommand());
        this.addSubCommand(new InfoSubCommand());
        this.addSubCommand(new ReloadSubCommand());
        this.addSubCommand(new UISubCommand());
    }

    @Override
    protected boolean canGeneratePermission() {
        return false; // No permission required for base command
    }
}