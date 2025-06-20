using UnityEngine;
using TMPro;

public class GameResultRow : MonoBehaviour
{
    public TMP_Text homeTeamText;
    public TMP_Text awayTeamText;
    public TMP_Text homeScoreText;
    public TMP_Text awayScoreText;

    public void SetData(GameResult result)
    {
        if (result == null) return;
        if (homeTeamText != null) homeTeamText.text = result.home;
        if (awayTeamText != null) awayTeamText.text = result.away;
        if (homeScoreText != null) homeScoreText.text = result.home_score.ToString();
        if (awayScoreText != null) awayScoreText.text = result.away_score.ToString();
    }
}
