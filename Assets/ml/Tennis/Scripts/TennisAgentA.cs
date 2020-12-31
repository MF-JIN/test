using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace ml.Tennis
{
    public class TennisAgentA : Agent
    {
        #region Properties

        public TennisPlayground playground;
        public Rigidbody rb;

        public uint score;
        public uint hitCount;

        public float scale;
        public bool invertX;
        public float m_InvertMult;
        public float m_velocityMax = 9; // 百米世界纪录不到 10m/s
        public float m_rotateMax = 180f;

        [Tooltip("最佳击球高度")]
        public float BestTargetY = 0.9f;
        /// <summary>
        /// 目标点
        /// </summary>
        public Vector3 Tp;
    
        /// <summary>
        /// 预判击球点
        /// </summary>
        public Vector3 Hp;

        /// <summary>
        /// 时间
        /// </summary>
        public float Tt; 
        
        [Header("v_3:r_4")] public List<float> m_Actions;
    
        public EnvironmentParameters EnvParams => Academy.Instance.EnvironmentParameters;
  
        public Action episodeBeginAction;
        #endregion

        #region MyRegion

        /// <summary>
        /// 最佳击球点
        /// </summary>
        /// <returns>[0,1,2]: 落地前, 第一次落地点, 第一次弹起后, 第二次落地前</returns>
        public Vector3[] GetTargetPos()
        {
            // var ball = playground.ball;
            // var G = playground.G;
            // Tp = ball.GetTargetPos(0.6f);
            
            return new []{Vector3.back};
        }
        #endregion

        #region Agent

        // [Header("i_1:p_3:r_4:v_3:l_1:lbr_4:bp_3")]
        // public List<float> Observations;
        public override void Initialize() // OnEnable
        {
            m_InvertMult = invertX ? -1f : 1f;

            Reset();
        }


        /// <summary>
        /// <br/>为了使代理学习，观察应包括代理完成其任务所需的所有信息。如果没有足够的相关信息，座席可能会学得不好或根本不会学。
        /// <br/>确定应包含哪些信息的合理方法是考虑计算该问题的分析解决方案所需的条件，或者您希望人类能够用来解决该问题的方法。<br/>
        /// <br/>产生观察
        /// <br/>   ML-Agents为代理提供多种观察方式：
        /// <br/>
        /// <br/>重写Agent.CollectObservations()方法并将观测值传递到提供的VectorSensor。
        /// <br/>将[Observable]属性添加到代理上的字段和属性。
        /// <br/>ISensor使用SensorComponent代理的附件创建来实现接口ISensor。
        /// <br/>Agent.CollectObservations（）
        /// <br/>Agent.CollectObservations（）最适合用于数字和非可视环境。Policy类调用CollectObservations(VectorSensor sensor)每个Agent的 方法。
        /// <br/>此函数的实现必须调用VectorSensor.AddObservation添加矢量观测值。
        /// <br/>该VectorSensor.AddObservation方法提供了许多重载，可将常见类型的数据添加到观察向量中。
        /// <br/>您可以直接添加整数和布尔值，以观测向量，以及一些常见的统一数据类型，如Vector2，Vector3和Quaternion。
        /// <br/>有关各种状态观察功能的示例，您可以查看ML-Agents SDK中包含的 示例环境。例如，3DBall示例使用平台的旋转，球的相对位置和球的速度作为状态观察。
        /// </summary>
        /// <param name="sensor" type="VectorSensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_InvertMult); // 角色 x1
            
            sensor.AddObservation(playground.Size); // x3

            sensor.AddObservation(playground.ball.transform.localPosition); // 球位置 x3
            sensor.AddObservation(playground.ball.rb.velocity); // 球速度 x3
            // sensor.AddObservation(playground.ball.rb.angularVelocity); // 角速度 x3
            
            // agentA
            sensor.AddObservation(playground.agentA.transform.localPosition);
            sensor.AddObservation(playground.agentA.rb.velocity);
            
            // agentB
            sensor.AddObservation(playground.agentB.transform.localPosition);
            sensor.AddObservation(playground.agentB.rb.velocity);
        }

        /**
     * 动作是代理执行的来自策略的指令。当学院调用代理的OnActionReceived()功能时，该操作将作为参数传递给代理。
     * 代理的动作可以采用两种形式之一，即Continuous或Discrete。
     * 当您指定矢量操作空间为Continuous时，传递给Agent的action参数是长度等于该Vector Action Space Size属性的浮点数数组。
     * 当您指定 离散向量动作空间类型时，动作参数是一个包含整数的数组。每个整数都是命令列表或命令表的索引。
     * 在离散向量操作空间类型中，操作参数是索引数组。数组中的索引数由Branches Size属性中定义的分支数确定。
     * 每个分支对应一个动作表，您可以通过修改Branches 属性来指定每个表的大小。
     * 策略和训练算法都不了解动作值本身的含义。训练算法只是为动作列表尝试不同的值，并观察随着时间的推移和许多训练事件对累积奖励的影响。
     * 因此，仅在OnActionReceived()功能中为代理定义了放置动作。
     * 例如，如果您设计了一个可以在两个维度上移动的代理，则可以使用连续或离散矢量动作。
     * 在连续的情况下，您可以将矢量操作大小设置为两个（每个维一个），并且座席的策略将创建一个具有两个浮点值的操作。
     * 在离散情况下，您将使用一个分支，其大小为四个（每个方向一个），并且策略将创建一个包含单个元素的操作数组，其值的范围为零到三。
     * 或者，您可以创建两个大小为2的分支（一个用于水平移动，一个用于垂直移动），并且Policy将创建一个包含两个元素的操作数组，其值的范围从零到一。
     * 请注意，在为代理编程动作时，使用代理Heuristic()方法测试动作逻辑通常会很有帮助，该方法可让您将键盘命令映射到动作。
     */

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var continuousActions = actionBuffers.ContinuousActions;
            #if UNITY_EDITOR
            m_Actions = continuousActions.ToList();
            #endif

            int i = 0;
            var velocityX = Mathf.Clamp(continuousActions[i++], -1f, 1f) * m_velocityMax;
            var velocityY = Mathf.Clamp(continuousActions[i++], -1f, 1f) * m_velocityMax;
            var velocityZ = Mathf.Clamp(continuousActions[i++], -1f, 1f) * m_velocityMax;
            var rotateX   = Mathf.Clamp(continuousActions[i++], -1f, 1f) * m_rotateMax;
            var rotateY   = Mathf.Clamp(continuousActions[i++], -1f, 1f) * m_rotateMax;
            var rotateZ   = Mathf.Clamp(continuousActions[i++], -1f, 1f) * m_rotateMax;
            // var rotateW     = Mathf.Clamp(continuousActions[i++], -1f, 1f);

            // // 不干预决策，在 TennisBall 中限制球的运动范围来引导
            // if (playground.agentA.score < playground.levelOne || playground.agentB.score < playground.levelOne)
            // {
            //     rotateX = invertX ? 180f : 0f;
            //     rotateY = 0f;
            //     velocityZ = 0f;
            // }

            rb.velocity = new Vector3(velocityX, velocityY, velocityZ);

            // 
            rb.rotation =
                Quaternion.Euler(new Vector3(rotateX, rotateY, rotateZ)); // 这比使用Transform.rotation更新旋转速度更快
            // or 
            // transform.localEulerAngles = new Vector3(rotateX, rotateY, rotateZ);

        }


        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var ball = playground.ball;
            var offset = transform.rotation.normalized * new Vector3(0f, 0f, -1.6f);
            var p0 = transform.localPosition + offset; // 拍心

            // transform.localPosition = ball.GetTargetPos() - offset;
            var velocity = (ball.GetTargetPos() - p0) / ball.Tt;
            rb.velocity = velocity;

            var rotation = Quaternion.LookRotation(velocity, Vector3.right);
            transform.rotation = rotation;
            // rigidbody.rotation = rotation;

            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[0] = velocity.x; // velocityX Racket Movement
            continuousActionsOut[1] = velocity.y; // velocityY Racket Jumping
            continuousActionsOut[2] = velocity.z; // velocityZ
            continuousActionsOut[3] = rotation.x; // rotateX
            continuousActionsOut[4] = rotation.y; // rotateY
            continuousActionsOut[5] = rotation.z; // rotateZ

            // continuousActionsOut[0] = Input.GetAxis("Horizontal");              // moveX Racket Movement
            // continuousActionsOut[1] = Input.GetKey(KeyCode.Space) ? 1f : 0f;    // moveY Racket Jumping
            // continuousActionsOut[2] = Input.GetAxis("Vertical");                // moveZ
            // if(SystemInfo.supportsGyroscope)
            // {
            //     var ang = Input.gyro.attitude.eulerAngles;
            //     continuousActionsOut[3] = Input.gyro.attitude.x; // rotateX
            //     continuousActionsOut[4] = Input.gyro.attitude.y; // rotateY
            //     continuousActionsOut[5] = Input.gyro.attitude.z; // rotateZ
            //     // continuousActionsOut[6] = Input.gyro.attitude.w; // rotateW
            // }
            // else
            // {
            //     continuousActionsOut[0] = Random.Range(-1f, 1f); // moveX Racket Movement
            //     continuousActionsOut[1] = Random.Range(-1f, 1f); // moveY Racket Jumping
            //     continuousActionsOut[2] = Random.Range(-1f, 1f); // moveZ
            //     continuousActionsOut[3] = Random.Range(-1f, 1f); // rotateX
            //     continuousActionsOut[4] = Random.Range(-1f, 1f); // rotateY
            //     continuousActionsOut[5] = Random.Range(-1f, 1f); // rotateZ
            //     // continuousActionsOut[6] = Random.Range(-1f, 1f); // rotateW
            // }

        }


        public override void OnEpisodeBegin()
        {
            Reset();

            if (episodeBeginAction != null)
                episodeBeginAction();
        }

        public void Reset()
        {
            transform.localPosition = new Vector3(
                -m_InvertMult * 8,
                2f,
                m_InvertMult * 1.5f);

            rb.velocity = new Vector3(0f, 0f, 0f);
            rb.rotation = Quaternion.Euler(new Vector3(
                invertX ? 180f : 0f,
                0f,
                -55f
            ));
        }

        #endregion //Agent

        #region MonoBehaviour

        // private void FixedUpdate()
        // {
        //     var p = transform.localPosition; // GetBallTargetP(); //
        //     // var rp = rigidbody.position;
        //     transform.localPosition = new Vector3(
        //         Mathf.Clamp(p.x, 2f * (invertX ? 0f : -playground.Size.x), 2f * (invertX ? playground.Size.x : 0f)),
        //         Mathf.Clamp(p.y, 0.1f, 3f),
        //         Mathf.Clamp(p.z, -2f * playground.Size.z, 2f * playground.Size.z));
        //     
        //     // rigidbody.rotation = Quaternion.Euler(new Vector3(
        //     //     invertX ? 180f : 0f,
        //     //     0f,
        //     //     -55f
        //     // ));
        // }

        #endregion //MonoBehaviour
    }
}