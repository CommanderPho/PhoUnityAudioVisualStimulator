using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // add to the top

public class StimulationTracker
{
    float startTime = 0f;
    float elapsedTime = 0f;
}


public abstract class StimulationWaveformHelper
{
    public float stimulationStartTime = 0f; // The time of the first frame of stimulation was started (in seconds)
    public float elapsedStimulationDuration = 0f; // The length of time since the first frame that the stimulation has been running.
    public StimulationWaveformHelper() {}

    public abstract bool updateStimulation(float current_time); // Returns true if the state changed
    public abstract float updateStimulationLevel(float current_time);
    // For continuous functions you wouldn't need to call updateStimulation(...) because you know the value is always changing, and instead you can just call updateStimulationLevel(...) to get the new value.
}



public abstract class StimulationWaveformHelper_SquareWave : StimulationWaveformHelper
{
    public enum SquareWaveState {LOW, HIGH};
    public float square_wave_HIGH_duration = 1.0f;
    public float square_wave_LOW_duration = 1.0f;
    public float square_wave_HIGH_value = 1.0f;
    public float square_wave_LOW_value = 0.0f;
    public float total_cycle_duration {
        get {
            return (this.square_wave_LOW_duration + this.square_wave_HIGH_duration);
        }
    }

    public SquareWaveState currentState = SquareWaveState.LOW;
    public float currentStateDuration {
        get {
            switch (this.currentState)
            {
            case SquareWaveState.LOW:
                return this.square_wave_LOW_duration;
                break;
            case SquareWaveState.HIGH:
                return this.square_wave_HIGH_duration;
                break;            
            default:
                Debug.LogError("Unimplemented case!!");
                return 0.0f;
                break;
            } // end switch
        }
    }

    public float currentStateValue {
        get {
            switch (this.currentState)
            {
            case SquareWaveState.LOW:
                return this.square_wave_LOW_value;
                break;
            case SquareWaveState.HIGH:
                return this.square_wave_HIGH_value;
                break;            
            default:
                Debug.LogError("Unimplemented case!!");
                return 0.0f;
                break;
            } // end switch
        }
    }

    // Debug Variables:
    protected int number_expected_transitions {
        get {
            float num_elapsed_cycles = this.elapsedStimulationDuration / this.total_cycle_duration; // note this is potentially a float number.
            return (int)num_elapsed_cycles;
        }
    }

    protected int number_actual_observed_state_transitions = 0;

    public int number_missed_transitions {
        get {
            return (this.number_expected_transitions - this.number_actual_observed_state_transitions);
        }
    }
    public SquareWaveState getState(float percent_cycle_offset)
    {
        // The percent of the current cycle that's completed
        float cycle_time_offset = percent_cycle_offset * this.total_cycle_duration;
        if (cycle_time_offset < this.square_wave_LOW_duration)
        {
            return SquareWaveState.LOW;
        }
        else {
            return SquareWaveState.HIGH;
        }
    }

    protected float last_transition_time = 0.0f; // Time the last transition occured.
    public StimulationWaveformHelper_SquareWave(float on_duration, float off_duration) : base() {
        this.square_wave_HIGH_duration = on_duration;
        this.square_wave_LOW_duration = off_duration;
        this.last_transition_time = 0.0f;
    }


    public override float updateStimulationLevel(float current_time)
    {
        // bool did_stimulation_value_change = this.updateStimulation(current_time);
        return this.currentStateValue;
    }

    public override bool updateStimulation(float current_time) 
    {
        // Check if it's the first frame:
        if (this.stimulationStartTime == 0f) {
            this.stimulationStartTime = current_time;
        }

        // Update the elapsed stim duration.
        this.elapsedStimulationDuration = current_time - this.stimulationStartTime;

        float num_elapsed_cycles = this.elapsedStimulationDuration / this.total_cycle_duration; // note this is potentially a float number.
        int num_full_elapsed_cycles = (int)num_elapsed_cycles; // Get the full number of cycles missed
        // float curr_fractional_portion_of_cycle = num_elapsed_cycles - (this.total_cycle_duration * (float)num_full_elapsed_cycles); // The remaining offset into the current cycle.
        float curr_fractional_portion_of_cycle = num_elapsed_cycles - num_full_elapsed_cycles; // The remaining offset into the current cycle.

        SquareWaveState desiredState = this.getState(curr_fractional_portion_of_cycle);
        bool state_will_change = (this.currentState != desiredState);

        if (state_will_change) 
        {
            this.currentState = desiredState;
            this.last_transition_time = current_time;
            this.number_actual_observed_state_transitions++;
            return true;
        }
        else {
            // state did not change.
            return false;
        }

        // float elapsedTimeSinceStateChange = this.last_transition_time - current_time; // update the elapsed time
        
        // // Checked for completely missed cycles:
        // if (elapsedTimeSinceStateChange < currentStateDuration)
        // {
        //     return; // No change
        // }
        // else {

        // }
    }
}

public class StimulationFunction_40Hz : StimulationWaveformHelper_SquareWave
{
        // Want 12.5ms on, then 12.5 ms off.
    public StimulationFunction_40Hz() : base((12.5f/1000.0f), (12.5f/1000.0f)) {}



}

public class CriticalityStimulator : MonoBehaviour
{
    public CanvasGroup myCG;
    public GameObject overlayPanel;

    protected Image overlayImageComponent;


    public GameObject debugPanel;
    public GameObject hudTextFramesDroppedGameObject;
    protected TMPro.TextMeshProUGUI txtFramesDropped;


    protected GameObject btnToggleVisualStim_GO;
    protected Button btnToggleVisualStim;
    protected GameObject btnToggleAudioStim_GO;
    protected Button btnToggleAudioStim;
    // private bool flash = false;

    public GameObject proceduralAudioControllerGameObject;
    protected ProceduralAudioController audioController;

    private StimulationFunction_40Hz stimulatorMan = new StimulationFunction_40Hz();

    private bool is_visual_flash_active = false;
    private bool is_audio_stimulation_active = false;

    // Start is called before the first frame update
    void Start()
    {
        // Get the attached overlayImageComponent:
        this.overlayImageComponent = this.overlayPanel.GetComponent<Image>();
        this.txtFramesDropped = this.hudTextFramesDroppedGameObject.GetComponent<TMPro.TextMeshProUGUI>();
        this.txtFramesDropped.text = "0";
        this.audioController = this.proceduralAudioControllerGameObject.GetComponent<ProceduralAudioController>();
        this.audioController.enabled = this.is_audio_stimulation_active;

        // var target = transform.Find("HUD_Overlay_Canvas/DebugPanel/Subpanel/ButtonWrapper/btnToggleVisualStim");
        this.btnToggleVisualStim_GO = this.debugPanel.transform.Find("Subpanel/ButtonWrapper/btnToggleVisualStim").gameObject;
        this.btnToggleVisualStim = this.btnToggleVisualStim_GO.GetComponent<Button>();
        this.btnToggleAudioStim_GO = this.debugPanel.transform.Find("Subpanel/ButtonWrapper/btnToggleAudioStim").gameObject;
        this.btnToggleAudioStim = this.btnToggleAudioStim_GO.GetComponent<Button>();

        this.UpdateButtonToggleState();
        // this.debugPanel.GetComponentsInChildren<GameObject>()  
    }

    // Refreshes the visual highlighting of the "Toggle Stimulation" buttons.
    void UpdateButtonToggleState()
    {
        if (this.is_visual_flash_active) {
            this.btnToggleVisualStim.GetComponent<Image>().color = new Color(0.0f, 0.186f, 0.0f, 1.0f); // set the custom active block to green.
        }
        else {
            this.btnToggleVisualStim.GetComponent<Image>().color = new Color(0.106f, 0.106f, 0.106f, 1.0f); 
        }

        if (this.is_audio_stimulation_active)
        {
            this.btnToggleAudioStim.GetComponent<Image>().color = new Color(0.0f, 0.186f, 0.0f, 1.0f); // set the custom active block to green.
        }
        else {
            this.btnToggleAudioStim.GetComponent<Image>().color = new Color(0.106f, 0.106f, 0.106f, 1.0f); 
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (this.is_visual_flash_active)
        {
            
            bool did_state_change = this.stimulatorMan.updateStimulation(Time.time);
            if (did_state_change) {
                float new_value = this.stimulatorMan.currentStateValue;
                // myCG.alpha = new_value;

                this.overlayImageComponent.color = new Color((255 * new_value), (255 * new_value), (255 * new_value));

                this.txtFramesDropped.text = this.stimulatorMan.number_missed_transitions.ToString();
            }
        }
    }


     public void ToggleFlashing()
     {
         this.is_visual_flash_active = !this.is_visual_flash_active;
         this.UpdateButtonToggleState();
         // TODO: Update to start/stop flashing
        //  if (this.is_visual_flash_active)
        //  {
        //      StartCoroutine(CriticalityStimulator.FadeCanvas(myCG, 1f, 0f, 2f));
        //  }
        //  else {
        //      StartCoroutine(CriticalityStimulator.FadeCanvas(myCG, 0f, 1f, 2f));
        //  }
     }

    // Turns the Audio Stimulation sound on or off
     public void ToggleAudio()
     {
         this.is_audio_stimulation_active = !this.is_audio_stimulation_active;
         this.audioController.enabled = this.is_audio_stimulation_active;
         this.UpdateButtonToggleState();
     }

    public IEnumerator FadeThenShowButtons()
    {
        // start fading
        yield return StartCoroutine(CriticalityStimulator.FadeCanvas(myCG, 1f, 0f, 2f));
        // code here will run once the fading coroutine has completed
        // myButton.enabled = true;
    }
    public static IEnumerator FadeCanvas(CanvasGroup canvas, float startAlpha, float endAlpha, float duration)
    {
        // keep track of when the fading started, when it should finish, and how long it has been running&lt;/p&gt; &lt;p&gt;&a
        var startTime = Time.time;
        var endTime = Time.time + duration;
        var elapsedTime = 0f;

        // set the canvas to the start alpha – this ensures that the canvas is ‘reset’ if you fade it multiple times
        canvas.alpha = startAlpha;
        // loop repeatedly until the previously calculated end time
        while (Time.time <= endTime)
        {
            elapsedTime = Time.time - startTime; // update the elapsed time
            var percentage = 1/(duration/elapsedTime); // calculate how far along the timeline we are
            if (startAlpha > endAlpha) // if we are fading out/down 
            {
                canvas.alpha = startAlpha - percentage; // calculate the new alpha
            }
            else // if we are fading in/up
            {
                canvas.alpha = startAlpha + percentage; // calculate the new alpha
            }

            yield return new WaitForEndOfFrame(); // wait for the next frame before continuing the loop
        }
        canvas.alpha = endAlpha; // force the alpha to the end alpha before finishing – this is here to mitigate any rounding errors, e.g. leaving the alpha at 0.01 instead of 0
}

}
