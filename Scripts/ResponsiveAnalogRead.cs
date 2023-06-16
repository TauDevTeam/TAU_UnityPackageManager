using System;

public class ResponsiveAnalogRead
{
    public ResponsiveAnalogRead()
    {
    } //default constructor must be followed by call to begin function
    public ResponsiveAnalogRead(int pin, bool sleepEnable, float snapMultiplier = 0.01f)
    {
        begin(pin, sleepEnable, snapMultiplier);
    }

    public void begin(int pin, bool sleepEnable, float snapMultiplier = 0.01f)
    {
        this.pin = pin;
        this.sleepEnable = sleepEnable;
        setSnapMultiplier(snapMultiplier);
    }

    public int getValue()
    {
        return responsiveValue;
    } // get the responsive value from last update
    public int getRawValue()
    {
        return rawValue;
    } // get the raw analogRead() value from last update
    public bool hasChanged()
    {
        return responsiveValueHasChanged;
    } // returns true if the responsive value has changed during the last update
    public bool isSleeping()
    {
        return sleeping;
    } // returns true if the algorithm is currently in sleeping mode

    public void update(int rawValueRead)
    {
        rawValue = rawValueRead;
        prevResponsiveValue = responsiveValue;
        responsiveValue = getResponsiveValue(rawValue);
        responsiveValueHasChanged = responsiveValue != prevResponsiveValue;
    }

    public void setSnapMultiplier(float newMultiplier)
    {
        if (newMultiplier > 1.0f)
        {
            newMultiplier = 1.0f;
        }
        if (newMultiplier < 0.0f)
        {
            newMultiplier = 0.0f;
        }
        snapMultiplier = newMultiplier;
    }

    public void enableSleep()
    {
        sleepEnable = true;
    }
    public void disableSleep()
    {
        sleepEnable = false;
    }
    public void enableEdgeSnap()
    {
        edgeSnapEnable = true;
    }
    // edge snap ensures that values at the edges of the spectrum (0 and 1023) can be easily reached when sleep is enabled
    public void disableEdgeSnap()
    {
        edgeSnapEnable = false;
    }
    public void setActivityThreshold(float newThreshold)
    {
        activityThreshold = newThreshold;
    }
    // the amount of movement that must take place to register as activity and start moving the output value. Defaults to 4.0
    public void setAnalogResolution(int resolution)
    {
        analogResolution = resolution;
    }
    // if your ADC is something other than 10bit (1024), set that here

    private int pin;
    private int analogResolution = 1024;
    private float snapMultiplier;
    private bool sleepEnable;
    private float activityThreshold = 4.0f;
    private bool edgeSnapEnable = true;

    private float smoothValue;
    private uint lastActivityMS;
    private float errorEMA = 0.0F;
    private bool sleeping = false;

    private int rawValue;
    private int responsiveValue;
    private int prevResponsiveValue;
    private bool responsiveValueHasChanged;

    private int getResponsiveValue(int newValue)
    {
        // if sleep and edge snap are enabled and the new value is very close to an edge, drag it a little closer to the edges
        // This'll make it easier to pull the output values right to the extremes without sleeping,
        // and it'll make movements right near the edge appear larger, making it easier to wake up
        if (sleepEnable && edgeSnapEnable)
        {
            if (newValue < activityThreshold)
            {
                newValue = (int)((newValue * 2) - activityThreshold);
            }
            else if (newValue > analogResolution - activityThreshold)
            {
                newValue = (int)((newValue * 2) - analogResolution + activityThreshold);
            }
        }

        // get difference between new input value and current smooth value
        uint diff = (uint)Math.Abs(newValue - smoothValue);

        // measure the difference between the new value and current value
        // and use another exponential moving average to work out what
        // the current margin of error is
        errorEMA += ((newValue - smoothValue) - errorEMA) * 0.4f;

        // if sleep has been enabled, sleep when the amount of error is below the activity threshold
        if (sleepEnable)
        {
            // recalculate sleeping status
            sleeping = Math.Abs(errorEMA) < activityThreshold;
        }

        // if we're allowed to sleep, and we're sleeping
        // then don't update responsiveValue this loop
        // just output the existing responsiveValue
        if (sleepEnable && sleeping)
        {
            return (int)smoothValue;
        }

        // use a 'snap curve' function, where we pass in the diff (x) and get back a number from 0-1.
        // We want small values of x to result in an output close to zero, so when the smooth value is close to the input value
        // it'll smooth out noise aggressively by responding slowly to sudden changes.
        // We want a small increase in x to result in a much higher output value, so medium and large movements are snappy and responsive,
        // and aren't made sluggish by unnecessarily filtering out noise. A hyperbola (f(x) = 1/x) curve is used.
        // First x has an offset of 1 applied, so x = 0 now results in a value of 1 from the hyperbola function.
        // High values of x tend toward 0, but we want an output that begins at 0 and tends toward 1, so 1-y flips this up the right way.
        // Finally the result is multiplied by 2 and capped at a maximum of one, which means that at a certain point all larger movements are maximally snappy

        // then multiply the input by SNAP_MULTIPLER so input values fit the snap curve better.
        float snap = snapCurve(diff * snapMultiplier);

        // when sleep is enabled, the emphasis is stopping on a responsiveValue quickly, and it's less about easing into position.
        // If sleep is enabled, add a small amount to snap so it'll tend to snap into a more accurate position before sleeping starts.
        if (sleepEnable)
        {
            snap *= 0.5f + 0.5f;
        }

        // calculate the exponential moving average based on the snap
        smoothValue += (newValue - smoothValue) * snap;

        // ensure output is in bounds
        if (smoothValue < 0.0f)
        {
            smoothValue = 0.0f;
        }
        else if (smoothValue > analogResolution - 1)
        {
            smoothValue = analogResolution - 1;
        }

        // expected output is an integer
        return (int)smoothValue;
    }

    private float snapCurve(float x)
    {
        float y = 1.0f / (x + 1.0f);
        y = (1.0f - y) * 2.0f;
        if (y > 1.0f)
        {
            return 1.0f;
        }
        return y;
    }
}
