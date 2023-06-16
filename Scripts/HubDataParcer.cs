using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TAU_OpenGlove;
using UnityEngine;
using static TAU_OpenGlove.TauStructs;


/*
  #[inline]
    pub fn unit_x() -> Vector3<S> {
        Vector3::new(S::one(), S::zero(), S::zero())
    }

    /// A unit vector in the `y` direction.
    #[inline]
    pub fn unit_y() -> Vector3<S> {
        Vector3::new(S::zero(), S::one(), S::zero())
    }

    /// A unit vector in the `z` direction.
    #[inline]
    pub fn unit_z() -> Vector3<S> {
        Vector3::new(S::zero(), S::zero(), S::one())
    }
*/


public struct CallibrationMinMax
{
    public int fingerID;
    public float ud_MIN;
    public float ud_MAX;
    public float signKoef;

    public void CalcSignKoef()
    {
        this.signKoef = 1f;

        if (ud_MIN > ud_MAX)
        {
            this.signKoef = -1f;
        }

        if (ud_MIN < ud_MAX)
        {
            this.signKoef = -1f;
        }
    }
}


public class TAU_HandData
{
    public bool isRIGHT = true;
    public CallibrationMinMax[] calibs;
    public float[] lastFingersCurl;
    public ResponsiveAnalogRead[] responsiveFingers;
    public Quaternion rawHandQuat;
    public Quaternion[] rawFngersQuats;
    public float[] rawQuatFingerAxis;

    public bool callibrateMin = false;
    public bool callibrateMax = false;

    public TAU_HandData(bool isRightHand)
    {
        Debug.Log("CONSTRUCT TAU HAND...");
        isRIGHT = isRightHand;
        rawQuatFingerAxis = new float[5];
        rawHandQuat = Quaternion.identity;
        rawFngersQuats = new Quaternion[5];
        calibs = new CallibrationMinMax[5];
        lastFingersCurl = new float[5];
        responsiveFingers = new ResponsiveAnalogRead[5];

        for (int i = 0; i < 5; i++)
        {
            responsiveFingers[i] = new ResponsiveAnalogRead();
            responsiveFingers[i].begin(0, false, 0.01f);
            responsiveFingers[i].setActivityThreshold(10f);
        }
    }
}


public class HubDataParcer : MonoBehaviour
{
    [Header("0 - LEFT / 1 - RIGHT")]
    public TAU_HandData[] TAU_Hands;

    DataPacket datapacket = DataPacket.empty();
    Dictionary<ushort, string> cached_mapping = new Dictionary<ushort, string>();

    InputData left_hand_input;
    InputData right_hand_input;
    InputData[] inputs;


    // public float[] rawQuatFingerAxis = { 0f, 0f, 0f, 0f, 0f };


    public int debIndex = 0;

    public PULSO_Handpad hand_R;
    public PULSO_Handpad hand_L;

    public Vector3[] V3_X = {
            new Vector3(1f,0f,0f),
            new Vector3(1f,0f,0f)
        };

    public Vector3[] V3_Y = {
            new Vector3(0f,1f,0f),
            new Vector3(0f,1f,0f)
        };

    public Vector3[] V3_Z = {
            new Vector3(0f,0f,1f),
            new Vector3(0f,0f,1f)
        };

    [TextArea(3, 10)]
    public string lastDataR = "";

    [TextArea(3, 10)]
    public string lastDataL = "";


    public float angle_koef = 210.0f;
    public float angle_koef_other = 120;
    public float ud_angle_minus = -30f;
    public float uv_snap_angle = 70.0f;

    public bool calibration_mode_bool = false;
    public bool gestures_enabled_bool = true;

    [TextArea(3, 10)]
    public string UDS = "";

    public CallibrationMinMax[] calibs = new CallibrationMinMax[5];

    public static HubDataParcer instance;


    private void Awake()
    {
        instance = this;
    }


    public static float Round(float value, int digits)
    {
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }

    private void Start()
    {
        //rawQuatFingerAxis = new float[] { 0f, 0f, 0f, 0f, 0f };
        TAU_Hands = new TAU_HandData[2] { new TAU_HandData(false), new TAU_HandData(true) };

        left_hand_input = new InputData(true);
        right_hand_input = new InputData(true);
        inputs = new InputData[] { left_hand_input, right_hand_input };
    }




    public Transform[] debFingers;
    public Transform debHand;


    public void CallibrateMin()
    {
        TAU_Hands[0].callibrateMin = true;
        TAU_Hands[1].callibrateMin = true;
    }


    public void CallibrateMax()
    {
        TAU_Hands[0].callibrateMax = true;
        TAU_Hands[1].callibrateMax = true;
    }


    float GetXDegrees(Transform t)
    {
        // Get the angle about the world x axis in range -pi to +pi,
        // with 0 corresponding to a 180 degree rotation.
        var radians = Mathf.Atan2(t.forward.x, -t.forward.z);

        // Map to range from 0 to 360 degrees,
        // with 0 corresponding to no rotation.
        return 180 + radians * Mathf.Rad2Deg;
    }



    private void LateUpdate()
    {

        //Debug.Log(GetXDegrees(debFinger));

        if (Input.GetKeyDown(KeyCode.N))
        {
            CallibrateMin();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            CallibrateMax();
        }

        foreach (TAU_HandData t in TAU_Hands)
        {


            for (int i = 0; i < 5; i++)
            {
                if (t.responsiveFingers[i] != null)
                {
                    t.responsiveFingers[i].update((int)(1024 * t.rawQuatFingerAxis[i]));

                    int f1RespINT = t.responsiveFingers[i].getValue();
                    float f1F = Round(t.responsiveFingers[i].getValue() / 1024f, 2);

                    if (t.isRIGHT)
                    {
                        if (hand_R != null)
                        {
                            hand_R.fingers[i].rootNodeAngle_01 = 1f - f1F;
                        }
                    }
                    else
                    {
                        if (hand_L != null)
                        {
                            hand_L.fingers[i].rootNodeAngle_01 = 1f - f1F;
                        }
                    }
                    //   else
                    //  {
                    //hand_R.fingers[i].rootNodeAngle_01 = f1F;

                    t.lastFingersCurl[i] = f1F;
                    // }
                }

                //  debFingers[i].rotation = t.rawHandQuat * t.rawFngersQuats[i];

                //var q = debFingers[i].rotation.eulerAngles;
                //q.y = 0f;
                // q.z = 0f;
                //debFingers[i].rotation = Quaternion.Euler(q);
            }
        }

        // debHand.rotation = t.rawHandQuat;
    }


    public void ParceData(byte[] dataPacket)
    {
        //Debug.Log("PARCWE");
        #region DATA_TYPE
        var cur = new BinaryReader(new MemoryStream(dataPacket));
        cur.BaseStream.Position = 2;

        var _packet_id = cur.ReadUInt16();

        int _current_page = cur.ReadByte();
        int _total_pages = cur.ReadByte();

        // Get packet options and their byte size
        byte[] opts = new byte[3];
        int[] szs = new int[3];

        for (int x = 0; x < opts.Length; x++)
        {
            opts[x] = (byte)cur.ReadByte();
        }

        for (int x = 0; x < szs.Length; x++)
        {
            if (opts[x] > 0)
            {
                szs[x] = cur.ReadUInt16();
            }
        }

        //Debug.LogFormat("opts0= {0} szs0= {1}", opts[0], szs[0]);
        //Debug.LogFormat("opts1= {0} szs1= {1}", opts[1], szs[1]);
        //Debug.LogFormat("opts2= {0} szs2= {1}", opts[2], szs[2]);

        // Parse data block
        if (opts[0] > 0)
        {
            byte[] vec_buffer = new byte[(int)szs[0]];
            vec_buffer = Helpers.ReadExact(cur.BaseStream, vec_buffer.Length);
            datapacket = DataPacket.FromBytes(
                vec_buffer,
                cached_mapping,
                1
            );
        }

        if (opts[1] > 0)
        {
            byte[] vec_buffer = new byte[(int)szs[1]];
            vec_buffer = Helpers.ReadExact(cur.BaseStream, vec_buffer.Length);
        }

        if (opts[2] > 0)
        {
            byte[] vec_buffer = new byte[(int)szs[2]];

            vec_buffer = Helpers.ReadExact(cur.BaseStream, vec_buffer.Length);

            Dictionary<ushort, string> mapping = new Dictionary<ushort, string>();
            string buf_txt = Encoding.UTF8.GetString(vec_buffer);
            string[] buf_txt_split_lines = buf_txt.Split('\n');
            foreach (string sl in buf_txt_split_lines)
            {
                string[] buf_txt_split_val = sl.Split('=');
                ushort cur_val = 0;
                ushort sens_id = 0;
                string sens_mapping = "";
                foreach (string sv in buf_txt_split_val)
                {
                    if (sv != "")
                    {
                        if (cur_val == 0)
                        {
                            bool parse_sens_id_attempt = ushort.TryParse(sv, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sens_id);
                            if (parse_sens_id_attempt)
                            {
                                //Ok;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (cur_val == 1)
                        {
                            sens_mapping = sv.Trim('"').ToString();
                        }
                        cur_val++;
                    }
                }

                if (sens_id != 0)
                {
                    mapping.Add(sens_id, sens_mapping);
                }
                cached_mapping = mapping;
            }
        }

        #endregion
        #region FINGERS_GEOMETRY

        string[] map_hands = { "left_arm", "right_arm" };
        Debug.Log("tyt");
        for (int h = 0; h < map_hands.Length; h++)
        {
            string map_hand = map_hands[h];

            float[] curl_vals = new float[5];

            Sensor hand_sensor = datapacket.get_sensor_by_mapping(string.Format("{0}{1}", map_hand, ".hand"));

            if (hand_sensor.IsEmpty() == false)
            {
                InputData hand_input = inputs[h];
                string[] mappings =
                {
                    string.Format("{0}{1}", map_hand, ".hand.thumb"),
                    string.Format("{0}{1}", map_hand, ".hand.index"),
                    string.Format("{0}{1}", map_hand, ".hand.middle"),
                    string.Format("{0}{1}", map_hand, ".hand.ring"),
                    string.Format("{0}{1}", map_hand, ".hand.pinky"),

                };

                Quaternion mod_q = hand_sensor.quat();

                TAU_Hands[h].rawHandQuat = mod_q;

                Quaternion mod_q_inv = Quaternion.Inverse(mod_q);
                Debug.Log("tuy2");
                string udsNew = "";
                for (int i = 0; i < mappings.Length; i++)
                {
                    Debug.Log(i);
                    Sensor right_index_sensor = datapacket.get_sensor_by_mapping(mappings[i]);

                    #region PARSE_FINGERS
                    if (right_index_sensor.IsEmpty() == false)
                    {
                        Quaternion sens_q = right_index_sensor.quat();
                        var diff = mod_q_inv * sens_q;

                        TAU_Hands[h].rawFngersQuats[i] = diff;

                        #region BIG_FINGER
                        if (i == 0)
                        {
                            Vector3 proj = Helpers.ProjectVector3OnPlane_new(
                              diff * V3_X[0],
                              V3_Z[0]
                          );


                            float ud_angle = Helpers.SignedAngleBetweenVector3(
                                proj,
                                V3_X[0],
                                V3_Z[0]
                            );


                            if (h == 0)
                            {
                                ud_angle = -ud_angle;
                            }

                            ud_angle -= 30.0f; // TODO: make configurable
                            float curl_val = -ud_angle / 120.0f; // TODO: make configurable
                            curl_vals[i] = curl_val;

                            /*
                            Vector3 proj = Helpers.ProjectVector3OnPlane_new
                            (
                                Helpers.rotate_vector_by_quaternion(V3_RIGHT[0], diff),
                                V3_FORWARD[0]
                            );


                            float ud_angle = Helpers.SignedAngleBetweenVector3
                            (
                                proj,
                                V3_UP[0],
                                V3_FORWARD[0]
                            );
                            */


                            /*
                            udsNew += i + ":  " + ud_angle + "\n";

                            if (callibrateMin)
                            {
                                calibs[i].ud_MIN = ud_angle;
                            }
                            else if (callibrateMax)
                            {
                                calibs[i].ud_MAX = ud_angle;
                            }

                            rawQuatFingerAxis[0] = MapFloat(ud_angle, calibs[i].ud_MIN, calibs[i].ud_MAX, 0f, 1f);

                            */

                            Debug.Log("tyt3");
                            udsNew += i + ":  " + diff.y + " " + diff.z + "\n";

                            if (TAU_Hands[h].callibrateMin)
                            {
                                TAU_Hands[h].calibs[i].ud_MIN = diff.y;
                            }
                            else if (TAU_Hands[h].callibrateMax)
                            {
                                TAU_Hands[h].calibs[i].ud_MAX = diff.y;
                            }

                            float resultF = MapFloat(diff.y, TAU_Hands[h].calibs[i].ud_MIN, TAU_Hands[h].calibs[i].ud_MAX, 0f, 1f);
                            if (resultF > 1f)
                            {
                                resultF = 1f;
                            }

                            if (resultF < 0f)
                            {
                                resultF = 0f;
                            }


                            TAU_Hands[h].rawQuatFingerAxis[0] = resultF;

                            //hand_R.fingers[0].rootNodeAngle_01 = MapFloat(ud_angle, calibs[i].ud_MIN, calibs[i].ud_MAX, 0f, 1f);
                        }
                        #endregion
                        #region OTHER_FINGERS
                        else
                        {
                            /*
                            let proj = project_vector3_on_plane(
                                           diff * Vector3::unit_x(),
                                           Vector3::unit_y(),

                                       );
                            let mut ud_angle = signed_angle_between_vector3(
                                proj,
                                Vector3::unit_x(),
                                Vector3::unit_y(),

                            );
                            if ud_angle > uv_snap_angle {
                                ud_angle = -360.0 + ud_angle;
                            }
                            let curl_val = -ud_angle / 210.0;
                            curl_vals[i] = curl_val;
                            */


                            Vector3 proj = Helpers.ProjectVector3OnPlane_new(
                            diff * V3_X[0],
                            V3_Y[0]
                        );
                            ///вся эта хуета не работает

                            float ud_angle = Helpers.SignedAngleBetweenVector3(
                                proj,
                                V3_X[0],
                                V3_Y[0]
                            );

                            /*
                            if (ud_angle > uv_snap_angle)
                            {
                                ud_angle = -360.0f + ud_angle;
                            }
                            float curl_val = -ud_angle / 210.0f;

                            */

                            // udsNew += i + ":  " + proj.x + " " + proj.y + " " + proj.z + "\n";

                            ///////////////////
                            udsNew += i + ": min " + TAU_Hands[h].calibs[i].ud_MIN + " max " + TAU_Hands[h].calibs[i].ud_MAX + "  current " + diff.x + "\n";
                            Debug.Log("yty4");
                            if (TAU_Hands[h].callibrateMin)
                            {
                                TAU_Hands[h].calibs[i].ud_MIN = diff.x;
                            }
                            else if (TAU_Hands[h].callibrateMax)
                            {
                                TAU_Hands[h].calibs[i].ud_MAX = diff.x;
                            }


                            float resultF = MapFloat(diff.x, TAU_Hands[h].calibs[i].ud_MIN, TAU_Hands[h].calibs[i].ud_MAX, 0f, 1f);
                            if (resultF > 1f)
                            {
                                resultF = 1f;
                            }

                            if (resultF < 0f)
                            {
                                resultF = 0f;
                            }

                            TAU_Hands[h].rawQuatFingerAxis[i] = resultF;
                            //hand_R.fingers[i].rootNodeAngle_01 = 1f - resultF;

                        }
                        #endregion
                    }
                }

                UDS = udsNew;

                /*
                if (TAU_Hands[h].callibrateMax || TAU_Hands[h].callibrateMin)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        TAU_Hands[h].calibs[i].CalcSignKoef();

                        Debug.LogWarning(i + " sign koef " + TAU_Hands[h].calibs[i].signKoef);
                    }
                }
                */
                Debug.Log("ttt5");
                TAU_Hands[h].callibrateMax = false;
                TAU_Hands[h].callibrateMin = false;
                #endregion

                if (h == 0)
                {
                    lastDataL = Helpers.PrintInputs(hand_input, "Left");
                }
                else
                {
                    Debug.Log("RRRRR");
                    lastDataR = Helpers.PrintInputs(hand_input, "Right");

                }

                UpdateInteractives();
            }
        }
        #endregion
    }


    void UpdateInteractives()
    {
        // Жесты
        float l_cap = 0.30f; // TODO: make configurable
        float h_cap = 0.70f; // TODO: make configurable




        /*
        if (curl_vals[2] > h_cap
            && curl_vals[3] > h_cap
            && curl_vals[4] > h_cap
            && calibration_mode_bool)
        {
            hand_input.calibrate = true;

            // Средний, безымянный сжаты, мизинец расжат
            // TODO: make configurable
        }

        if (gestures_enabled_bool && !calibration_mode_bool)
        {
            // Кнопка триггера - указательный сжат
            // TODO: make configurable
            hand_input.trgValue = curl_vals[1] * 1.00f;

            if (curl_vals[0] > 0.92f)
            {
                // TODO: make configurable
                if (h == 0)
                {
                    hand_input.joyY = 1.0f;
                }
                else
                {
                    hand_input.joyButton = true;
                }
            }

            if (curl_vals[1] > 0.80f)
            {
                //TODO: make configurable
                hand_input.trgButton = true;
            }


            // Средний, безымянный, мизинец - сжаты
            // TODO: make configurable
            if (curl_vals[2] > h_cap && curl_vals[3] > h_cap && curl_vals[4] > h_cap)
            {
                hand_input.grab = true;

                // Средний, безымянный сжаты, мизинец расжат
                // TODO: make configurable
            }
            else if (curl_vals[2] > h_cap
              && curl_vals[3] > h_cap
              && curl_vals[4] < l_cap)
            {
                hand_input.aButton = true;
            }
        }
        */
    }


    float MapFloat(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
}
