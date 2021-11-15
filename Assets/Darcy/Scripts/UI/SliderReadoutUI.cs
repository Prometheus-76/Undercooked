using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Author: Darcy Matheson
// Purpose: Displays the modified value of the sensitivity slider on the pause screen

public class SliderReadoutUI : MonoBehaviour
{
    private TextMeshProUGUI dataLabel;
    private Slider dataSlider;

    // Start is called before the first frame update
    void Start()
    {
        dataSlider = transform.parent.GetComponent<Slider>();
        dataLabel = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        dataLabel.text = (dataSlider.value / 10f).ToString("F1");
    }
}
