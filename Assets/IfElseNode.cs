using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using System.Collections;

namespace TreeSharpPlus
{
    /// <summary>
    /// Evaluates a lambda function. if evaluate to true the execute the If Node,
    /// else execute the ElseNode
    /// </summary>
    public class IfElseNode : NodeGroup
    {
        protected Func<bool> func_assert = null;
        protected Node IfNode;
        protected Node ElseNode;

        public IfElseNode(Func<bool> assertion, Node trueNode, Node falseNode)
        {
            this.func_assert = assertion;
            this.IfNode = trueNode;
            this.ElseNode = falseNode;
        }

        public override IEnumerable<RunStatus> Execute()
        {
            if (this.func_assert != null)
            {
                bool bool_result = this.func_assert.Invoke();
                RunStatus exec_result;

                //Debug.Log(result);
                if (bool_result == true)
                {
                    this.IfNode.Start();

                    // If the current node is still running, report that. Don't 'break' the enumerator
                    while ((exec_result = this.TickNode(this.IfNode)) == RunStatus.Running)
                        yield return RunStatus.Running;

                    // Call Stop to allow the node to clean anything up.
                    this.IfNode.Stop();
                    yield return exec_result;
                }
                else
                {
                    this.ElseNode.Start();

                    // If the current node is still running, report that. Don't 'break' the enumerator
                    while ((exec_result = this.TickNode(this.ElseNode)) == RunStatus.Running)
                        yield return RunStatus.Running;

                    // Call Stop to allow the node to clean anything up.
                    this.ElseNode.Stop();
                    yield return exec_result;
                }

                yield break;
            }
            else
            {
                throw new ApplicationException(this + ": No method given");
            }
        }
    }
}