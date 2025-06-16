import random
from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA


def generate_player_dna(position: str, level: str = "pro") -> PlayerDNA:
    """Convenience wrapper to produce a ``PlayerDNA`` instance."""
    return PlayerDNA.generate_random_dna(position, level=level)


def generate_player_attributes_from_caps(dna: PlayerDNA, bias: str = "average") -> dict:
    """Create starting attributes biased for pro, college or free agents."""
    attr_dict = {}
    for attr, cap_data in dna.attribute_caps.items():
        soft = cap_data["soft_cap"]
        hard = cap_data["hard_cap"]

        if bias == "pro":
            base = int(random.normalvariate(soft, 3))
        elif bias == "college":
            base = soft - random.randint(10, 30)
        elif bias == "free_agent":
            base = soft - random.randint(15, 40)
        else:
            base = soft - random.randint(5, 20)

        attr_dict[attr] = max(40, min(base, hard))
    return attr_dict


def generate_pro_player(position: str, age: int) -> dict:
    dna = generate_player_dna(position, level="pro")
    attributes = generate_player_attributes_from_caps(dna, bias="pro")
    return {
        "position": position,
        "age": age,
        "attributes": attributes,
        "dna": dna,
        "origin": "pro",
    }


def generate_college_player(position: str, age: int = 21) -> dict:
    dna = generate_player_dna(position, level="college")
    attributes = generate_player_attributes_from_caps(dna, bias="college")
    return {
        "position": position,
        "age": age,
        "attributes": attributes,
        "dna": dna,
        "origin": "college",
    }


def generate_free_agent(position: str, age: int) -> dict:
    dna = generate_player_dna(position, level="pro")
    attributes = generate_player_attributes_from_caps(dna, bias="free_agent")
    return {
        "position": position,
        "age": age,
        "attributes": attributes,
        "dna": dna,
        "origin": "free_agent",
    }
