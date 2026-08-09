package hytale.template.nenilearn.listeners;

import com.hypixel.hytale.event.EventRegistry;
import com.hypixel.hytale.logger.HytaleLogger;
import com.hypixel.hytale.server.core.event.events.player.PlayerConnectEvent;
import com.hypixel.hytale.server.core.event.events.player.PlayerDisconnectEvent;

import java.util.logging.Level;

/**
 * Listener for player connection events.
 *
 * Listens to:
 * - PlayerConnectEvent - When a player connects to the server
 * - PlayerDisconnectEvent - When a player disconnects from the server
 */
public class PlayerListener {

    private static final HytaleLogger LOGGER = HytaleLogger.forEnclosingClass();

    /**
     * Register all player event listeners.
     * @param eventBus The event registry to register listeners with
     */
    public void register(EventRegistry eventBus) {
        // PlayerConnectEvent - When a player connects
        try {
            eventBus.register(PlayerConnectEvent.class, this::onPlayerConnect);
            LOGGER.at(Level.INFO).log("[NeniLearn] Registered PlayerConnectEvent listener");
        } catch (Exception e) {
            LOGGER.at(Level.WARNING).withCause(e).log("[NeniLearn] Failed to register PlayerConnectEvent");
        }

        // PlayerDisconnectEvent - When a player disconnects
        try {
            eventBus.register(PlayerDisconnectEvent.class, this::onPlayerDisconnect);
            LOGGER.at(Level.INFO).log("[NeniLearn] Registered PlayerDisconnectEvent listener");
        } catch (Exception e) {
            LOGGER.at(Level.WARNING).withCause(e).log("[NeniLearn] Failed to register PlayerDisconnectEvent");
        }
    }

    /**
     * Handle player connect event.
     * @param event The player connect event
     */
    private void onPlayerConnect(PlayerConnectEvent event) {
        String playerName = event.getPlayerRef() != null ? event.getPlayerRef().getUsername() : "Unknown";
        String worldName = event.getWorld() != null ? event.getWorld().getName() : "unknown";

        LOGGER.at(Level.INFO).log("[NeniLearn] Player %s connected to world %s", playerName, worldName);

        // TODO: Add your player join logic here
        // Examples:
        // - Send welcome message
        // - Load player data
        // - Announce join to other players
    }

    /**
     * Handle player disconnect event.
     * @param event The player disconnect event
     */
    private void onPlayerDisconnect(PlayerDisconnectEvent event) {
        String playerName = event.getPlayerRef() != null ? event.getPlayerRef().getUsername() : "Unknown";

        LOGGER.at(Level.INFO).log("[NeniLearn] Player %s disconnected", playerName);

        // TODO: Add your player leave logic here
        // Examples:
        // - Save player data
        // - Announce leave to other players
        // - Clean up player resources
    }
}