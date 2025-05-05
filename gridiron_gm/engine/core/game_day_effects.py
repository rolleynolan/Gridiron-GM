import random

class GameDayEffects:
    def __init__(self):
        pass

    def apply_pre_game_effects(self, team):
        """
        Apply any morale/fatigue boosts or drops prior to a game.
        """
        for player in team.roster:
            self._apply_morale_effect(player)
            self._apply_fatigue_decay(player)

    def apply_post_game_effects(self, team, win):
        """
        Update player morale and fatigue after the game outcome.
        """
        for player in team.roster:
            # Fatigue increases after playing
            player.fatigue = min(100, player.fatigue + random.randint(5, 15))

            # Morale adjustment
            if win:
                player.morale = min(100, player.morale + random.randint(1, 5))
            else:
                player.morale = max(0, player.morale - random.randint(1, 5))

    def _apply_fatigue_decay(self, player):
        """
        Reduce fatigue over the week.
        """
        recovery_rate = 10
        if "High Work Ethic" in player.traits.get("training", []):
            recovery_rate += 5
        player.fatigue = max(0, player.fatigue - recovery_rate)

    def _apply_morale_effect(self, player):
        """
        Minor morale adjustments pre-game depending on traits.
        """
        if "Media Favorite" in player.traits.get("media", []):
            player.morale = min(100, player.morale + 1)
        elif "Distracted" in player.traits.get("mental", []):
            player.morale = max(0, player.morale - 1)
