using System;
using System.IO;
using UnityEngine;

namespace UABS.Assets.Script.__Test__.TextAsset
{
    public class Xlsx2Txt : MonoBehaviour
    {
        private string inputXlsxPath = "Assets/Experiment/Xlsx2Txt/file.xlsx";
        private string outputTxtPath = "Assets/Experiment/Xlsx2Txt/Encoded.txt";

        private void Start()
        {
            EncodeXLSXToTxt();
        }

        private void EncodeXLSXToTxt()
        {
            if (!File.Exists(inputXlsxPath))
            {
                Debug.LogError($"File not found: {inputXlsxPath}");
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(inputXlsxPath);
                string base64 = Convert.ToBase64String(bytes);
                File.WriteAllText(outputTxtPath, base64);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to encode TXT: {e}");
            }
        }
    }
}