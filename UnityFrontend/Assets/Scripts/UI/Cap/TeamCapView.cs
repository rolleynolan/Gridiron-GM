using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gridiron.Bridge.Validation;
using Gridiron.Core;

namespace Gridiron.UI.Cap
{
    internal class TeamCapView : MonoBehaviour
    {
        [SerializeField] private string teamAbbr;
        [SerializeField] private int year;
        [SerializeField] private TMP_Text banner;
        [SerializeField] private TMP_Text rowTemplate;
        [SerializeField] private Transform tableParent;

        private void OnEnable()
        {
            if (tableParent == null)
            {
                var go = new GameObject("CapTable", typeof(RectTransform), typeof(VerticalLayoutGroup));
                go.transform.SetParent(transform, false);
                tableParent = go.transform;
            }

            if (rowTemplate == null)
            {
                var rowObj = new GameObject("RowTemplate", typeof(RectTransform), typeof(TMP_Text));
                rowObj.transform.SetParent(tableParent, false);
                rowTemplate = rowObj.GetComponent<TMP_Text>();
                rowTemplate.gameObject.SetActive(false);
            }

            try
            {
                var path = string.Format("data/cap/capsheet_{0}.json", year);
                var sheet = DataIO.LoadJson<CapsheetDTO>(path);
                Render(sheet);
            }
            catch (GGDataException ex)
            {
                GGLog.Warn(ex.Message);
                ShowBanner("Data version mismatch (" + ex.Code + ")");
            }
            catch (IOException)
            {
                ShowBanner("Capsheet missing");
            }
        }

        private void Render(CapsheetDTO sheet)
        {
            foreach (Transform child in tableParent)
            {
                if (child != rowTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            if (sheet == null || sheet.Rows == null) return;

            foreach (var row in sheet.Rows)
            {
                var inst = Instantiate(rowTemplate, tableParent);
                inst.gameObject.SetActive(true);
                inst.text = string.Format("{0} | {1}", row.PlayerName, FormatMoney(row.CapHit));
            }

            if (sheet.Totals != null)
            {
                var inst = Instantiate(rowTemplate, tableParent);
                inst.gameObject.SetActive(true);
                inst.text = string.Format("TOTAL | {0}", FormatMoney(sheet.Totals.CapHit));
            }
        }

        private void ShowBanner(string msg)
        {
            if (banner == null)
            {
                var go = new GameObject("Banner", typeof(RectTransform), typeof(TMP_Text));
                go.transform.SetParent(transform, false);
                banner = go.GetComponent<TMP_Text>();
                banner.color = Color.red;
            }
            banner.text = msg;
        }

        private string FormatMoney(long v)
        {
            return "$" + (v / 1_000_000f).ToString("0.0") + "M";
        }

        [Serializable]
        private class CapsheetDTO
        {
            [Serializable]
            public class Row
            {
                public string PlayerName;
                public string TeamAbbr;
                public int Year;
                public long Base;
                public long SigningProrated;
                public long RosterBonus;
                public long WorkoutBonus;
                public long CapHit;
                public long DeadCap;
            }

            public List<Row> Rows;
            public Row Totals;
        }
    }
}
