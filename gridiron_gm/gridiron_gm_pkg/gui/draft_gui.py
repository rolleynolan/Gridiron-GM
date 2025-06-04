import sys
import json
import os
from PyQt6.QtWidgets import (
    QApplication, QWidget, QVBoxLayout, QListWidget, QTextEdit, QSplitter
)
from PyQt6.QtCore import Qt


class DraftBoard(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Draft Scouting")
        self.resize(800, 500)

        # Load JSON from the same folder as this script
        gui_dir = os.path.dirname(os.path.abspath(__file__))
        data_path = os.path.join(gui_dir, "draft_class.json")

        if not os.path.exists(data_path):
            self.players = []
        else:
            with open(data_path, "r", encoding="utf-8") as f:
                self.players = json.load(f)

        layout = QSplitter(Qt.Orientation.Horizontal)

        self.list_widget = QListWidget()
        for player in self.players:
            self.list_widget.addItem(f"{player['name']} ({player['position']})")
        self.list_widget.currentRowChanged.connect(self.show_player_report)

        self.report_box = QTextEdit()
        self.report_box.setReadOnly(True)

        layout.addWidget(self.list_widget)
        layout.addWidget(self.report_box)

        main = QVBoxLayout()
        main.addWidget(layout)
        self.setLayout(main)

    def show_player_report(self, index):
        if index < 0 or index >= len(self.players):
            self.report_box.setPlainText("")
            return

        player = self.players[index]
        report = f"""
Name: {player['name']}
Position: {player['position']}
Projected OVR: {player['overall_estimate']}
Dev Curve Guess: {player['dev_curve_guess']}
Role Projection: {player['role_projection']}

Scout Notes:
{player.get('commentary') or '—'}
        """.strip()

        self.report_box.setPlainText(report)


if __name__ == "__main__":
    app = QApplication(sys.argv)
    win = DraftBoard()
    win.show()
    sys.exit(app.exec())
