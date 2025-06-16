import pandas as pd
import seaborn as sns
import matplotlib.pyplot as plt
from pathlib import Path


def generate_position_attribute_histograms(csv_path: str | Path, output_dir: str | Path) -> None:
    """Generate histograms of all player attributes by position.

    Parameters
    ----------
    csv_path : str | Path
        Path to the ``player_generation_output.csv`` file.
    output_dir : str | Path
        Directory where the histogram images will be saved.
    """
    csv_path = Path(csv_path)
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    df = pd.read_csv(csv_path)

    exclude_keywords = ["_soft_cap", "_hard_cap"]
    attributes = [
        col
        for col in df.columns
        if all(k not in col for k in exclude_keywords)
        and col not in ["position", "level", "age"]
    ]

    for attr in attributes:
        for pos in df["position"].unique():
            subset = df[df["position"] == pos]
            plt.figure(figsize=(8, 5))
            sns.histplot(
                data=subset,
                x=attr,
                hue="level",
                kde=True,
                bins=25,
                stat="density",
                common_norm=False,
            )
            plt.title(f"{attr.capitalize()} Distribution - {pos}")
            plt.xlabel(attr.capitalize())
            plt.ylabel("Density")
            plt.tight_layout()
            plt.savefig(output_dir / f"{pos}_{attr}_distribution.png")
            plt.close()


__all__ = ["generate_position_attribute_histograms"]


if __name__ == "__main__":
    default_csv = Path("dna_output/player_generation_output.csv")
    default_out = Path("dna_output/attribute_graphs")
    generate_position_attribute_histograms(default_csv, default_out)
