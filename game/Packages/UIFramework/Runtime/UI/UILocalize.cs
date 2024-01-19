using UnityEngineEx;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class UILocalize : MonoBehaviour
{
    public string key;
    public string value
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Text.text = value;
            }
        }
    }

    private Text textComp;
    public Text Text
    {
        get
        {
            if (!textComp)
            {
                textComp = GetComponent<Text>();
            }
            return textComp;
        }
    }

#if UNITY_EDITOR
    public KeyValuePair<float, float> CalculateTextSize()
    {
        TextGenerator textGenerator = new TextGenerator();
        TextGenerationSettings widthTextGenerationSettings = Text.GetGenerationSettings(Vector2.zero);
        var width = textGenerator.GetPreferredWidth(Text.text, widthTextGenerationSettings) / Text.pixelsPerUnit;
        TextGenerationSettings heightTextGenerationSettings = Text.GetGenerationSettings(new Vector2(width, 0));
        var height = textGenerator.GetPreferredHeight(Text.text, heightTextGenerationSettings) / Text.pixelsPerUnit;

        return new KeyValuePair<float, float>(width, height);
    }
#endif

    // Use this for initialization
    void Awake()
    {
        if (!string.IsNullOrEmpty(key))
        {
            OnLocalize();
        }
        else
        {
            LanguageConverter.IterateText(transform);
        }
    }

    void OnLocalize()
    {
        if (!string.IsNullOrEmpty(key) && string.IsNullOrEmpty(Text.text))
        {
            value = LanguageConverter.GetLangValue(key);
        }
    }
}
