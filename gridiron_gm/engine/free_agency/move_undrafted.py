def move_undrafted_rookies_to_free_agents(game_world, free_agency_manager):
    """
    Move all undrafted rookies from the rookie class into the free agent pool.
    """
    undrafted = [player for player in game_world['rookie_class'] if not getattr(player, 'drafted', False)]
    for player in undrafted:
        free_agency_manager.add_free_agent(player)

    print(f"\n{len(undrafted)} undrafted rookies moved to free agency.")
