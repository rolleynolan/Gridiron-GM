using UnityEngine;
using TMPro;

public class PlayerRow : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text positionText;
    public TMP_Text ageText;
    public TMP_Text overallText;
    public TMP_Text contractText;

    public void SetData(PlayerInfo info)
    {
        if (info == null) return;
        if (nameText != null) nameText.text = info.name;
        if (positionText != null) positionText.text = info.position;
        if (ageText != null) ageText.text = info.age.ToString();
        if (overallText != null) overallText.text = info.overall.ToString();
        if (contractText != null)
        {
            int years = info.contract != null ? info.contract.years_left : 0;
            contractText.text = years.ToString();
        }
    }
}
