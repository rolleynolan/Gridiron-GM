using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gridiron.Bridge.Dto;

namespace Gridiron.UI.Cap
{
    internal class ContractDetailPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text rowTemplate;
        [SerializeField] private Transform tableParent;

        public void Show(ContractDTO contract, int currentYear)
        {
            if (tableParent == null)
            {
                var go = new GameObject("ContractTable", typeof(RectTransform), typeof(VerticalLayoutGroup));
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

            foreach (Transform child in tableParent)
            {
                if (child != rowTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            if (contract == null || contract.Terms == null) return;

            foreach (var term in contract.Terms)
            {
                if (term.Year == currentYear || term.Year == currentYear + 1)
                {
                    var inst = Instantiate(rowTemplate, tableParent);
                    inst.gameObject.SetActive(true);
                    inst.text = string.Format("{0}: {1}", term.Year, FormatMoney(CapHit(term)));
                }
            }
        }

        private long CapHit(ContractYearTerm t)
        {
            return t.Base + t.SigningProrated + t.RosterBonus + t.WorkoutBonus;
        }

        private string FormatMoney(long v)
        {
            return "$" + (v / 1_000_000f).ToString("0.0") + "M";
        }
    }
}
