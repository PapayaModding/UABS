using System;
using System.IO;
using UnityEngine;

namespace UABS.Assets.Script.__Test__.TextAsset
{
    public class TxtToXlsx : MonoBehaviour
    {
        // Path to your exported txt file (Base64)
        private string inputTxtPath = "Assets/Experiment/Txt2Xlsx/file.xlsx";

        // Output path for decoded .xlsx
        private string outputXLSXPath = "Assets/Experiment/Txt2Xlsx/Decoded.xlsx";

        private void Start()
        {
            DecodeTxtToXLSX();
        }

        private void DecodeTxtToXLSX()
        {
            if (!File.Exists(inputTxtPath))
            {
                Debug.LogError($"File not found: {inputTxtPath}");
                return;
            }

            try
            {
                // Read the Base64 text
                string base64Text = File.ReadAllText(inputTxtPath);

                // Decode Base64 to bytes
                byte[] xlsxBytes = Convert.FromBase64String(base64Text);

                // Save as .xlsx
                File.WriteAllBytes(outputXLSXPath, xlsxBytes);

                Debug.Log($"Decoded XLSX saved to: {outputXLSXPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to decode XLSX: {e}");
            }
        }
    }
}