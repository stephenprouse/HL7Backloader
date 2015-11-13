using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

/// <summary>
/// Represents an HL7 2.x message.
/// </summary>
/// <remarks>
/// <b>Revision History</b>
/// <table style="width: 100%;">
/// <tr>
/// <th style="width: 125px;">Date</th>
/// <th style="width: 200px;">Author</th>
/// <th>Description</th>
/// </tr>
/// <tr>
/// <td>20100224</td>
/// <td>Eric Carter</td>
/// <td>Created.</td>
/// </tr>
/// </table>
/// </remarks>
/// <example>
/// The following example demonstrates loading an HL7 message from a file and various processing functions.
/// <br/><br/>
/// <code>
/// Dim msg As New HL72Message()
///
/// msg.LoadFromFile("C:\Documents and Settings\ericc\Desktop\jmhmsg.txt")
/// 
/// 'in the third occurrence of IN1, set IN1-3 to "newval"
/// msg.SetValue("IN1", 2, 3, "newval") 
/// 
/// 'in the first occurrence of IN1, set the first repetition of IN1-3 to "repval1"
/// msg.SetValue("IN1", 0, 3, 0, "repval1") 
/// 
/// 'in the second occurrence of IN1, set the second repetition of IN1-3 to "repval2"
/// msg.SetValue("IN1", 0, 3, 1, "repval2") 
/// 
/// 'in the first occurrence of PID, set the first component of the the first component of the first repetition of PID-5 to "ERIC"
/// msg.SetValue("PID", 0, 5, 0, 0, "ERIC") 
/// 
/// 'in the first occurrence of DG1, set the first subcomponent of the the first component of the first repetition of DG1-8 to "A1"
/// msg.SetValue("DG1", 0, 8, 0, 0, 0, "A1") 
/// 
/// 'in the first occurrence of DG1, set the second subcomponent of the the first component of the first repetition of DG1-8 to "A2"
/// msg.SetValue("DG1", 0, 8, 0, 0, 1, "A2") 
///
/// 'in the first occurrence of DG1, set the third subcomponent of the the first component of the first repetition of DG1-8 to "A3"
/// msg.SetValue("DG1", 0, 8, 0, 0, 2, "A3") 
/// 
/// 'in the first occurrence of DG1, set the first subcomponent of the the second component of the first repetition of DG1-8 to "A1"
/// msg.SetValue("DG1", 0, 8, 0, 1, 0, "B1") 
/// 
/// 'in the first occurrence of DG1, set the second subcomponent of the the second component of the first repetition of DG1-8 to "A1"
/// msg.SetValue("DG1", 0, 8, 0, 1, 1, "B2") 
/// 
/// 'in the first occurrence of DG1, set the third subcomponent of the the second component of the first repetition of DG1-8 to "A1"
/// msg.SetValue("DG1", 0, 8, 0, 1, 2, "B3") 
///
/// 'insert a new PV2 segment at index 4 of the message
/// msg.InsertSegment(4, "PV2||||aaaaaa||||bbbbb|||c|")  
/// 
/// 'append an IN1 segment to the end of the message
/// msg.AppendSegment("IN1|4|||||||||||||||||||||||||||||||||||||||||||||")  
///
/// 'remove the segment at index 1 of the message
/// msg.RemoveSegmentAt(1) 
///
/// 'get MSH-2
/// Dim msh02 As String = msg.GetValue("MSH", 0, 2) 
/// 
/// 'get IN1-3 rep #2 from IN1 #1
/// Dim in1031 As String = msg.GetValue("IN1", 0, 3, 1) 
/// 
/// 'get PID-5-2
/// Dim pid0501 As String = msg.GetValue("PID", 0, 5, 0, 1) 
/// 
/// 'get DG1-8-1-2
/// Dim dg108011 As String = msg.GetValue("DG1", 0, 8, 0, 1, 1) 
///
/// 'show result by appending ZZZ segment 
/// Dim seg As New List(Of String)(New String() {"ZZZ", msh02, in1031, pid0501, dg108011})
/// msg.AppendSegment(seg)
/// 
/// 'strip out all IN1 segments
/// msg.RemoveAllSegments("IN1")
///
/// msg.Save("C:\Documents and Settings\ericc\Desktop\jmhmsg_new.txt", False)
/// </code>
/// </example>

internal class HL72Message
{

    #region "Member Variables"

    private string _fieldDelim;
    private string _repetitionDelim;
    private string _componentDelim;
    private string _subComponentDelim;
    private string _escapeChar;
    private List<string> _segments;
    private Dictionary<string, List<int>> _segmentIndices;

    private Dictionary<int, string[]> _splitSegments;
    #endregion

    #region "Constructors"

    /// <summary>
    /// Creates an empty HL7 2.x message without any segments defined.
    /// </summary>
    public HL72Message()
    {
        _fieldDelim = "|";
        _repetitionDelim = "~";
        _componentDelim = "^";
        _subComponentDelim = "&";
        _escapeChar = "\\";
        _segments = new List<string>();
        _segmentIndices = new Dictionary<string, List<int>>();
        _splitSegments = new Dictionary<int, string[]>();
    }

    #endregion

    #region "Public Methods"

    /// <summary>
    /// Clears any existing data from this message and loads the message using the given text.
    /// </summary>
    /// <param name="message">Text of the message to load.</param>
    public void Load(string message)
    {
        message = UnwrapMessage(message);

        if (message.Substring(0, 3) != "MSH")
            throw new InvalidOperationException("Message missing MSH as first segment.");

        _fieldDelim = message.Substring(3, 1);
        _componentDelim = message.Substring(4, 1);
        _repetitionDelim = message.Substring(5, 1);
        _escapeChar = message.Substring(6, 1);
        _subComponentDelim = message.Substring(7, 1);

        _segments.Clear();
        _segments.AddRange(message.Split((char)13));

        _splitSegments.Clear();

        //if the last split result is empty, remove it
        if (_segments[_segments.Count - 1].Length == 0)
            _segments.RemoveAt(_segments.Count - 1);

        UpdateSegmentIndicies();
    }

    /// <summary>
    /// Clears any existing data from this message and loads the message using the text found in the given file.
    /// </summary>
    /// <param name="filePath">Path of the file containing the message text.</param>
    public void LoadFromFile(string filePath)
    {
        string message = File.ReadAllText(filePath);
        Load(message);
    }

    /// <summary>
    /// Gets the index of the first occurrence of a segment in the message.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <returns>First index of the segment or -1 if no such segment exists.</returns>
    public int FirstIndexOfSegment(string segmentId)
    {
        if (_segmentIndices.ContainsKey(segmentId))
        {
            return _segmentIndices[segmentId][0];
        }
        else
        {
            return -1;
        }
    }

    /// <summary>
    /// Gets the first occurrence of a segment.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <returns>Full text of the segment, including delimiters but excluding the terminating carriage return.</returns>
    public string GetSegment(string segmentId)
    {
        return GetSegment(segmentId, 0);
    }

    /// <summary>
    /// Gets a specific occurrence of a segment.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to retrieve, starting with 0.</param>
    /// <returns>Full text of the segment, including delimiters but excluding the terminating carriage return.</returns>
    /// <remarks>If no such segment exists, the String.Empty is returned.</remarks>
    public string GetSegment(string segmentId, int segmentIndex)
    {
        if (_segmentIndices.ContainsKey(segmentId))
        {
            if (_segmentIndices[segmentId].Count > segmentIndex)
            {
                return _segments[_segmentIndices[segmentId][segmentIndex]];
            }
            else
            {
                return string.Empty;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Get all segments with a specific segment id.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <returns>List of segments with the given segment id.</returns>
    /// <remarks>Each segment will be the full text of the segment, including delimiters but excluding the terminating carriage return.</remarks>
    public List<string> GetSegments(string segmentId)
    {
        List<string> rtn = new List<string>();

        if (_segmentIndices.ContainsKey(segmentId))
        {
            foreach (int idx in _segmentIndices[segmentId])
            {
                rtn.Add(_segments[idx]);
            }
        }

        return rtn;
    }

    /// <summary>
    /// Get all segments in this message.
    /// </summary>
    /// <returns>List of all segments in the message.</returns>
    /// <remarks>This is a copy of the the internal segment list.  Changes made to the List returned by this method will not be reflected in the message.
    /// Each segment will be the full text of the segment, including delimiters but excluding the terminating carriage return.</remarks>
    public List<string> GetAllSegments()
    {
        List<string> rtn = new List<string>();

        foreach (string seg in _segments)
        {
            rtn.Add(seg);
        }

        return rtn;
    }

    /// <summary>
    /// Get all fields in a specific segment.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to retrieve, starting with 0.</param>
    /// <returns>Array of individual fields comprising the segment.  If no such segment exists the array will be empty.</returns>
    public string[] GetAllFields(string segmentId, int segmentIndex)
    {
        string[] flds = {

        };

        if (_segmentIndices.ContainsKey(segmentId))
        {
            if (_segmentIndices[segmentId].Count > segmentIndex)
            {
                int idx = _segmentIndices[segmentId][segmentIndex];

                //if we already split this seg, re-use the array
                if (_splitSegments.ContainsKey(idx))
                {
                    flds = _splitSegments[idx];
                }
                else
                {
                    flds = _segments[idx].Split(_fieldDelim.ToCharArray());

                    //save split for re-use
                    _splitSegments.Add(idx, flds);
                }
            }
        }

        return flds;
    }

    /// <summary>
    /// Gets the specified field in the first occurrence of a segment.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="fieldIndex">Index of the field to retrieve, starting with 0.</param>
    /// <returns>Text of the specified field.</returns>
    /// <remarks><seealso cref="GetValue">GetValue</seealso></remarks>
    public string GetField(string segmentId, int fieldIndex)
    {
        return GetField(segmentId, 0, fieldIndex);
    }

    /// <summary>
    /// Gets the specified field in the specified occurrence of a segment.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to retrieve, starting with 0.</param>
    /// <param name="fieldIndex">Occurrence of the field to retrieve, starting with 0.</param>
    /// <returns>Text of the specified field.</returns>
    /// <remarks><seealso cref="GetValue">GetValue</seealso></remarks>
    public string GetField(string segmentId, int segmentIndex, int fieldIndex)
    {
        string[] flds = GetAllFields(segmentId, segmentIndex);

        if (flds.Length > fieldIndex)
        {
            return flds[fieldIndex];
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Splits a field into its repetitions.
    /// </summary>
    /// <param name="field">Text of the field to split.</param>
    /// <returns>Array of field repetitions.</returns>
    public string[] GetAllRepetitionsFromField(string field)
    {
        return field.Split(_repetitionDelim.ToCharArray());
    }

    /// <summary>
    /// Gets a specific repetition from a field.
    /// </summary>
    /// <param name="field">Text of the field/</param>
    /// <param name="repetitionIndex">Occurrence of the repetition to retrieve, starting with 0.</param>
    /// <returns>Text of the specified field repetition.</returns>
    public string GetRepetitionFromField(string field, int repetitionIndex)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        string[] rpts = GetAllRepetitionsFromField(field);

        if (rpts.Length > repetitionIndex)
        {
            return rpts[repetitionIndex];
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Splits a field repetition into its components.
    /// </summary>
    /// <param name="repetition">Field repetition to split.</param>
    /// <returns>Array of components.</returns>
    public string[] GetAllComponentsFromRepetition(string repetition)
    {
        return repetition.Split(_componentDelim.ToCharArray());
    }

    /// <summary>
    /// Gets a specific component from a field repetition.
    /// </summary>
    /// <param name="repetition">Text of the field repetition.</param>
    /// <param name="componentIndex">Occurrence of the component to retrieve, starting with 0.</param>
    /// <returns>Text of the specified component.</returns>
    public string GetComponentFromRepetition(string repetition, int componentIndex)
    {
        if (string.IsNullOrEmpty(repetition))
            return string.Empty;

        string[] comps = GetAllComponentsFromRepetition(repetition);

        if (comps.Length > componentIndex)
        {
            return comps[componentIndex];
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Splits a component into its subcomponents.
    /// </summary>
    /// <param name="component">Component to split.</param>
    /// <returns>Array of subcomponents.</returns>
    public string[] GetAllSubComponentsFromComponent(string component)
    {
        return component.Split(_subComponentDelim.ToCharArray());
    }

    /// <summary>
    /// Gets a specific subcomponent from a component.
    /// </summary>
    /// <param name="component">Text of the component.</param>
    /// <param name="subComponentIndex">Occurrence of the subcomponent to retrieve, starting with 0.</param>
    /// <returns>Text of the specified subcomponent.</returns>
    public string GetSubComponentFromComponent(string component, int subComponentIndex)
    {
        if (string.IsNullOrEmpty(component))
            return string.Empty;

        string[] subcomps = GetAllSubComponentsFromComponent(component);

        if (subcomps.Length > subComponentIndex)
        {
            return subcomps[subComponentIndex];
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Get the value of a specific field.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be retrieved, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be retrieved, starting with 0 (0 is the segment id).</param>
    /// <returns>Text value of the field requested.</returns>
    public string GetValue(string segmentId, int segmentIndex, int fieldIndex)
    {
        return GetField(segmentId, segmentIndex, fieldIndex);
    }

    /// <summary>
    /// Get the value of a specific field repetition.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be retrieved, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be retrieved, starting with 0 (0 is the segment id).</param>
    /// <param name="repetitionIndex">Index of the field repetition to be retrieved, starting with 0.</param>
    /// <returns>Text value of the field repetition requested.</returns>
    public string GetValue(string segmentId, int segmentIndex, int fieldIndex, int repetitionIndex)
    {
        string field = GetValue(segmentId, segmentIndex, fieldIndex);

        return GetRepetitionFromField(field, repetitionIndex);
    }


    /// <summary>
    /// Get the value of a specific component.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be retrieved, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be retrieved, starting with 0 (0 is the segment id).</param>
    /// <param name="repetitionIndex">Index of the field repetition to be retrieved, starting with 0.</param>
    /// <param name="componentIndex">Index of the component to be retrieved, starting with 0.</param>
    /// <returns>Text value of the component requested.</returns>
    public string GetValue(string segmentId, int segmentIndex, int fieldIndex, int repetitionIndex, int componentIndex)
    {
        string rpt = GetValue(segmentId, segmentIndex, fieldIndex, repetitionIndex);

        return GetComponentFromRepetition(rpt, componentIndex);
    }

    /// <summary>
    /// Get the value of a specific subcomponent.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be retrieved, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be retrieved, starting with 0 (0 is the segment id).</param>
    /// <param name="repetitionIndex">Index of the field repetition to be retrieved, starting with 0.</param>
    /// <param name="componentIndex">Index of the component to be retrieved, starting with 0.</param>
    /// <param name="subComponentIndex">Index of the subcomponent to be retrieved, starting with 0.</param>
    /// <returns>Text value of the subcomponent requested.</returns>
    public string GetValue(string segmentId, int segmentIndex, int fieldIndex, int repetitionIndex, int componentIndex, int subComponentIndex)
    {
        string comp = GetValue(segmentId, segmentIndex, fieldIndex, repetitionIndex, componentIndex);

        return GetSubComponentFromComponent(comp, subComponentIndex);
    }


    /// <summary>
    /// Set the value of a specific field.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be changed, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be changed, starting with 0 (the seg id).</param>
    /// <param name="value">Text that will replace the current value of the field.</param>
    /// <remarks>If the field does not yet exist is will be created.</remarks>
    public void SetValue(string segmentId, int segmentIndex, int fieldIndex, string value)
    {
        string[] fields = GetAllFields(segmentId, segmentIndex);
        int idx = _segmentIndices[segmentId][segmentIndex];

        if (fields.Length > fieldIndex)
        {
            fields[fieldIndex] = value;
        }
        else
        {
            List<string> newFlds = new List<string>(fields);

            for (int i = 0; i <= fieldIndex; i++)
            {
                if (i > newFlds.Count - 1)
                {
                    newFlds.Add("");
                }
            }

            newFlds[fieldIndex] = value;
            fields = newFlds.ToArray();
        }

        //remove old split
        if (_splitSegments.ContainsKey(idx))
            _splitSegments.Remove(idx);

        _segments[idx] = EnumerableToString(fields, _fieldDelim);
    }

    /// <summary>
    /// Set the value of a specific field repetition.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be changed, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be changed, starting with 0 (the seg id).</param>
    /// <param name="repetitionIndex">Index of the field repetition to be changed, starting with 0.</param>
    /// <param name="value">Text that will replace the current value of the field repetition.</param>
    /// <remarks>If the field repetition does not yet exist is will be created.</remarks>
    public void SetValue(string segmentId, int segmentIndex, int fieldIndex, int repetitionIndex, string value)
    {
        string field = GetValue(segmentId, segmentIndex, fieldIndex);
        string[] rpts = GetAllRepetitionsFromField(field);

        if (rpts.Length > repetitionIndex)
        {
            rpts[repetitionIndex] = value;
        }
        else
        {
            List<string> newRpts = new List<string>(rpts);

            for (int i = 0; i <= repetitionIndex; i++)
            {
                if (i > newRpts.Count - 1)
                {
                    newRpts.Add("");
                }
            }

            newRpts[repetitionIndex] = value;
            rpts = newRpts.ToArray();
        }

        SetValue(segmentId, segmentIndex, fieldIndex, EnumerableToString(rpts, _repetitionDelim));
    }

    /// <summary>
    /// Set the value of a specific component.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be changed, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be changed, starting with 0 (0 is the segment id).</param>
    /// <param name="repetitionIndex">Index of the field repetition to be changed, starting with 0.</param>
    /// <param name="componentIndex">Index of the component to be changed, starting with 0.</param>
    /// <param name="value">Text that will replace the current value of the component.</param>
    /// <remarks>If the component does not yet exist is will be created.</remarks>
    public void SetValue(string segmentId, int segmentIndex, int fieldIndex, int repetitionIndex, int componentIndex, string value)
    {
        string rpt = GetValue(segmentId, segmentIndex, fieldIndex, repetitionIndex);
        string[] comps = GetAllComponentsFromRepetition(rpt);

        if (comps.Length > componentIndex)
        {
            comps[componentIndex] = value;
        }
        else
        {
            List<string> newComps = new List<string>(comps);

            for (int i = 0; i <= componentIndex; i++)
            {
                if (i > newComps.Count - 1)
                {
                    newComps.Add("");
                }
            }

            newComps[componentIndex] = value;
            comps = newComps.ToArray();
        }

        SetValue(segmentId, segmentIndex, fieldIndex, repetitionIndex, EnumerableToString(comps, _componentDelim));
    }

    /// <summary>
    /// Set the value of a specific subcomponent.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>
    /// <param name="segmentIndex">Occurrence of the segment to be changed, starting with 0.</param>
    /// <param name="fieldIndex">Index of the field to be changed, starting with 0 (0 is the segment id).</param>
    /// <param name="repetitionIndex">Index of the field repetition to be changed, starting with 0.</param>
    /// <param name="componentIndex">Index of the component to be changed, starting with 0.</param>
    /// <param name="subComponentIndex">Index of the subcomponent to be changed, starting with 0.</param>
    /// <param name="value">Text that will replace the current value of the subcomponent.</param>
    /// <remarks>If the subcomponent does not yet exist is will be created.</remarks>
    public void SetValue(string segmentId, int segmentIndex, int fieldIndex, int repetitionIndex, int componentIndex, int subComponentIndex, string value)
    {
        string comp = GetValue(segmentId, segmentIndex, fieldIndex, repetitionIndex, componentIndex);
        string[] subs = GetAllSubComponentsFromComponent(comp);

        if (subs.Length > subComponentIndex)
        {
            subs[subComponentIndex] = value;
        }
        else
        {
            List<string> newSubs = new List<string>(subs);

            for (int i = 0; i <= subComponentIndex; i++)
            {
                if (i > newSubs.Count - 1)
                {
                    newSubs.Add("");
                }
            }

            newSubs[subComponentIndex] = value;
            subs = newSubs.ToArray();
        }

        SetValue(segmentId, segmentIndex, fieldIndex, repetitionIndex, componentIndex, EnumerableToString(subs, _subComponentDelim));
    }

    /// <summary>
    /// Inserts a string representing an HL7 segment at the specified segment index in the message.
    /// </summary>
    /// <param name="index">Index at which to insert the segment.</param>
    /// <param name="segment">Text of the segment to be inserted, including delimiters but excluding the terminating carriage return.</param>
    public void InsertSegment(int index, string segment)
    {
        _segments.Insert(index, segment);

        UpdateSegmentIndicies();
    }

    /// <summary>
    /// Uses a List of fields to insert an HL7 segment at the specified segment index in the message.
    /// </summary>
    /// <param name="index">Index at which to insert the segment.</param>
    /// <param name="fields">List of fields used to comprise the new segment.</param>
    /// <remarks></remarks>
    public void InsertSegment(int index, List<string> fields)
    {
        _segments.Insert(index, EnumerableToString(fields, _fieldDelim));

        UpdateSegmentIndicies();
    }

    /// <summary>
    /// Appends a string representing an HL7 segment to the end of the message.
    /// </summary>
    /// <param name="segment">Text of the segment to be inserted, including delimiters but excluding the terminating carriage return.</param>
    public void AppendSegment(string segment)
    {
        _segments.Add(segment);

        UpdateSegmentIndicies();
    }

    /// <summary>
    /// Uses a List of fields to append an HL7 segment at the end of the message.
    /// </summary>
    /// <param name="fields">List of fields used to comprise the new segment.</param>
    public void AppendSegment(List<string> fields)
    {
        _segments.Add(EnumerableToString(fields, _fieldDelim));
    }

    /// <summary>
    /// Removes the segment at the specified index within the message.
    /// </summary>
    /// <param name="index">Index of the segment to be removed, starting with 0.</param>
    public void RemoveSegmentAt(int index)
    {
        _segments.RemoveAt(index);
        _splitSegments.Remove(index);

        UpdateSegmentIndicies();
    }

    /// <summary>
    /// Removes all segments from the message having the specified segment identifier.
    /// </summary>
    /// <param name="segmentId">Segment identifier (MSH, PID, etc.).</param>        
    public void RemoveAllSegments(string segmentId)
    {
        Stack<int> rmv = new Stack<int>();
        int i = 0;

        for (i = 0; i <= _segments.Count - 1; i++)
        {
            if (_segments[i].Substring(0, 3) == segmentId)
                rmv.Push(i);
        }

        while (rmv.Count > 0)
        {
            i = rmv.Pop();
            _segments.RemoveAt(i);
        }

        UpdateSegmentIndicies();
    }

    /// <summary>
    /// Renders this message as a single string of HL7-compliant text.
    /// </summary>
    /// <returns>This message in text form with all appropriate delimiters and segment terminators.</returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        foreach (string segment in _segments)
        {
            sb.Append(segment);
            sb.Append((char)13);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders this message as a single string of HL7-compliant text with the option to wrap the message for MLLP transmission.
    /// </summary>
    /// <param name="wrapForMllp">True if the message should be wrapped for MLLP transmission, otherwise false.</param>
    /// <returns>This message in text form with all appropriate delimiters and segment terminators, wrapped for MLLP transmission if so desired.</returns>
    public string ToString(bool wrapForMllp)
    {
        if (wrapForMllp)
        {
            return WrapMessage(this.ToString());
        }
        else
        {
            return this.ToString();
        }
    }

    /// <summary>
    /// Saves this message to a file.
    /// </summary>
    /// <param name="filePath">Path and filename of the file to be written.</param>
    /// <param name="wrapForMllp">True if the message should be wrapped for MLLP transmission, otherwise false.</param>
    /// <remarks>If the file already exists, it will be overwritten.</remarks>
    public void Save(string filePath, bool wrapForMllp)
    {
        string dir = Path.GetDirectoryName(filePath);

        //create dir if it doesn't already exist
        Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, this.ToString(wrapForMllp));
    }

    #endregion

    #region "Private Methods"

    private string EnumerableToString(IEnumerable<string> items, string delim)
    {
        StringBuilder sb = new StringBuilder();

        foreach (string item in items)
        {
            sb.Append(item);
            sb.Append(delim);
        }

        sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    private void UpdateSegmentIndicies()
    {
        _segmentIndices.Clear();

        for (int index = 0; index <= _segments.Count - 1; index++)
        {
            string segId = _segments[index].Substring(0, 3);

            if (!_segmentIndices.ContainsKey(segId))
            {
                _segmentIndices.Add(segId, new List<int>());
            }

            _segmentIndices[segId].Add(index);
        }
    }

    /// <summary>
    /// Wraps an HL7 message for MLLP transmission.
    /// </summary>
    /// <param name="message">HL7 message to evaluate and wrap if needed.</param>
    /// <returns>HL7 message wrapped in an MLLP envelope.</returns>
    /// <remarks>If the HL7 message is already wrapped in an MLLP envelope the message is returned unchanged.</remarks>
    private string WrapMessage(string message)
    {
        StringBuilder sb = new StringBuilder(message);

        //add SB to message if needed
        if (sb[0] != (char)11)
            sb.Insert(0, (char)11);

        if (sb[sb.Length - 1] == (char)28)
        {
            //append the proper trailing CR
            sb.Append((char)13);
        }
        else if (sb[sb.Length - 2] != (char)28)
        {
            //no EB, append it
            sb.Append((char)28);
            sb.Append((char)13);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Unwraps an HL7 message from its MLLP envelope.
    /// </summary>
    /// <param name="message">HL7 message to evaluate and unwrap if needed.</param>
    /// <returns>HL7 message without MLLP envelope.</returns>
    /// <remarks>If the HL7 message is already not wrapped in an MLLP envelope the message is returned unchanged.</remarks>
    private string UnwrapMessage(string message)
    {
        StringBuilder sb = new StringBuilder(message);

        //remove SB if present
        if (sb[0] == (char)11)
            sb.Remove(0, 1);

        //remove EBCR if present
        if (sb[sb.Length - 2] == (char)28)
        {
            sb.Remove(sb.Length - 2, 2);
        }
        else if (sb[sb.Length - 1] == (char)28)
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    #endregion

    #region "Properties"

    /// <summary>
    /// The HL7 2.x field delimiter used in this message.
    /// </summary>
    public string FieldDelimiter
    {
        get { return _fieldDelim; }
        set { _fieldDelim = value; }
    }

    /// <summary>
    /// The HL7 2.x field repetition delimiter used in this message.
    /// </summary>
    public string RepetitionDelimiter
    {
        get { return _repetitionDelim; }
        set { _repetitionDelim = value; }
    }

    /// <summary>
    /// The HL7 2.x component delimiter used in this message.
    /// </summary>
    public string ComponentDelimiter
    {
        get { return _componentDelim; }
        set { _componentDelim = value; }
    }

    /// <summary>
    /// The HL7 2.x subcomponent delimiter used in this message.
    /// </summary>
    public string SubComponentDelimiter
    {
        get { return _subComponentDelim; }
        set { _subComponentDelim = value; }
    }

    /// <summary>
    /// The HL7 2.x escape character used in this message.
    /// </summary>
    public string EscapeCharacter
    {
        get { return _escapeChar; }
        set { _escapeChar = value; }
    }

    /// <summary>
    /// The number of segments in this message.
    /// </summary>
    public int SegmentCount
    {
        get { return _segments.Count; }
    }

    #endregion

}
