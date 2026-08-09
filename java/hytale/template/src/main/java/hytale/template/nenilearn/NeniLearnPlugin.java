package hytale.template.nenilearn;

import com.hypixel.hytale.server.core.plugin.JavaPlugin;
import com.hypixel.hytale.server.core.plugin.JavaPluginInit;
import com.hypixel.hytale.event.EventRegistry;
import com.hypixel.hytale.logger.HytaleLogger;

import hytale.template.nenilearn.commands.NeniLearnPluginCommand;
import hytale.template.nenilearn.listeners.PlayerListener;

import javax.annotation.Nonnull;
import java.util.logging.Level;

/**
 * NeniLearn - A Hytale server plugin.
 */
public class NeniLearnPlugin extends JavaPlugin {

    private static final HytaleLogger LOGGER = HytaleLogger.forEnclosingClass();
    private static NeniLearnPlugin instance;

    public NeniLearnPlugin(@Nonnull JavaPluginInit init) {
        super(init);
        instance = this;
    }

    /**
     * Get the plugin instance.
     * @return The plugin instance
     */
    public static NeniLearnPlugin getInstance() {
        return instance;
    }

    @Override
    protected void setup() {
        LOGGER.at(Level.INFO).log("[NeniLearn] Setting up...");

        // Register commands
        registerCommands();

        // Register event listeners
        registerListeners();

        LOGGER.at(Level.INFO).log("[NeniLearn] Setup complete!");
    }

    /**
     * Register plugin commands.
     */
    private void registerCommands() {
        try {
            getCommandRegistry().registerCommand(new NeniLearnPluginCommand());
            LOGGER.at(Level.INFO).log("[NeniLearn] Registered /neni command");
        } catch (Exception e) {
            LOGGER.at(Level.WARNING).withCause(e).log("[NeniLearn] Failed to register commands");
        }
    }

    /**
     * Register event listeners.
     */
    private void registerListeners() {
        EventRegistry eventBus = getEventRegistry();

        try {
            new PlayerListener().register(eventBus);
            LOGGER.at(Level.INFO).log("[NeniLearn] Registered player event listeners");
        } catch (Exception e) {
            LOGGER.at(Level.WARNING).withCause(e).log("[NeniLearn] Failed to register listeners");
        }
    }

    @Override
    protected void start() {
        LOGGER.at(Level.INFO).log("[NeniLearn] Started!");
        LOGGER.at(Level.INFO).log("[NeniLearn] Use /neni help for commands");
    }

    @Override
    protected void shutdown() {
        LOGGER.at(Level.INFO).log("[NeniLearn] Shutting down...");
        instance = null;
    }
}