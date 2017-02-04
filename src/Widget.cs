/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// TGUI - Texus' Graphical User Interface
// Copyright (C) 2012-2016 Bruno Van de Velde (vdv_b@tgui.eu)
//
// This software is provided 'as-is', without any express or implied warranty.
// In no event will the authors be held liable for any damages arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it freely,
// subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented;
//    you must not claim that you wrote the original software.
//    If you use this software in a product, an acknowledgment
//    in the product documentation would be appreciated but is not required.
//
// 2. Altered source versions must be plainly marked as such,
//    and must not be misrepresented as being the original software.
//
// 3. This notice may not be removed or altered from any source distribution.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using SFML.System;

namespace TGUI
{
	public class Widget : SFML.ObjectBase
	{
		protected Widget(IntPtr cPointer)
			: base(cPointer)
		{
			InitSignals(); // Calls the function in the derived class
		}

		public Widget(Widget copy)
			: base(tguiWidget_copy(copy.CPointer))
		{
			InitSignals(); // Calls the function in the derived class
		}

		protected override void Destroy(bool disposing)
		{
			tguiWidget_destroy(CPointer);
		}

		public Vector2f Position
		{
			get { return tguiWidget_getPosition(CPointer); }
			set { tguiWidget_setPosition(CPointer, value); }
		}

		public void SetPosition(Layout2d layout)
		{
			tguiWidget_setPosition_fromLayout(CPointer, layout.CPointer);
		}

		public Vector2f AbsolultePosition
		{
			get { return tguiWidget_getAbsolutePosition(CPointer); }
		}

		public Vector2f Size
		{
			get { return tguiWidget_getSize(CPointer); }
			set { tguiWidget_setSize(CPointer, value); }
		}

		public void SetSize(Layout2d layout)
		{
			tguiWidget_setSize_fromLayout(CPointer, layout.CPointer);
		}

		public Vector2f FullSize
		{
			get { return tguiWidget_getFullSize(CPointer); }
		}

		public Vector2f WidgetOffset
		{
			get { return tguiWidget_getWidgetOffset(CPointer); }
		}

		public uint Connect(string signalName, Action func)
		{
			IntPtr error;
			uint id = tguiWidget_connect(CPointer, Util.ConvertStringForC_ASCII(signalName), () => func(), out error);
			if (error != IntPtr.Zero)
				throw new TGUIException(Util.GetStringFromC_ASCII(error));

			// Add the function to our dictionary
			signalName = signalName.ToLower();
			if (!myConnectedSignals.ContainsKey(signalName))
				myConnectedSignals[signalName] = new List<uint>();

			myConnectedSignals[signalName].Add(id);

			return id;
		}

		public uint Connect(string signalName, Action<Widget> func)
		{
			return Connect(signalName, () => func(this));
		}

		public void Disconnect(uint id)
		{
			tguiWidget_disconnect(CPointer, id);

			// Find and remove the signal with this id
			var signalNames = new List<string>(myConnectedSignals.Keys);
			foreach (var signalName in signalNames)
			{
				var idList = myConnectedSignals[signalName];
				if (idList.Contains(id))
				{
					if (idList.Count > 1)
						idList.Remove(id);
					else
						myConnectedSignals.Remove(signalName);

					break;
				}
			}
		}

		public void DisconnectAll(string signalName)
		{
			signalName = signalName.ToLower();
			if (myConnectedSignals.ContainsKey(signalName))
			{
				foreach (var id in myConnectedSignals[signalName])
					tguiWidget_disconnect(CPointer, id);

				myConnectedSignals.Remove(signalName);
			}
		}

		public void DisconnectAll()
		{
			foreach (var signal in myConnectedSignals)
			{
				foreach (var id in signal.Value)
					tguiWidget_disconnect(CPointer, id);
			}

			myConnectedSignals.Clear();
		}

		public WidgetRenderer Renderer
		{
			get { return new WidgetRenderer(tguiWidget_getRenderer(CPointer)); }
		}

		public void SetRenderer(RendererData renderer)
		{
			IntPtr error;
			tguiWidget_setRenderer(CPointer, renderer.CPointer, out error);
			if (error != IntPtr.Zero)
				throw new TGUIException(Util.GetStringFromC_ASCII(error));
		}

		public bool Visible
		{
			get { return tguiWidget_isVisible(CPointer); }
			set
			{
				if (value == true)
					tguiWidget_show(CPointer);
				else
					tguiWidget_hide(CPointer);
			}
		}

		public void ShowWithEffect(ShowAnimationType type, Time duration)
		{
			tguiWidget_showWithEffect(CPointer, type, duration);
		}

		public void HideWithEffect(ShowAnimationType type, Time duration)
		{
			tguiWidget_hideWithEffect(CPointer, type, duration);
		}

		public bool Enabled
		{
			get { return tguiWidget_isEnabled(CPointer); }
			set
			{
				if (value == true)
					tguiWidget_enable(CPointer);
				else
					tguiWidget_disable(CPointer);
			}
		}

		public string WidgetType
		{
			get { return Util.GetStringFromC_ASCII(tguiWidget_getWidgetType(CPointer)); }
		}

		public Container Parent
		{
			get
			{
				IntPtr ParentCPointer = tguiWidget_getParent(CPointer);
				if (ParentCPointer == IntPtr.Zero)
					return null;

				Type type = Type.GetType("TGUI." + Util.GetStringFromC_ASCII(tguiWidget_getWidgetType(ParentCPointer)));
				return (Container)Activator.CreateInstance(type, new object[]{ ParentCPointer });
			}
		}

		public void MoveToFront()
		{
			tguiWidget_moveToFront(CPointer);
		}

		public void MoveToBack()
		{
			tguiWidget_moveToBack(CPointer);
		}

		public Widget ToolTip
		{
			get
			{
				IntPtr ToolTipCPointer = tguiWidget_getToolTip(CPointer);
				if (ToolTipCPointer == IntPtr.Zero)
					return null;

				Type type = Type.GetType("TGUI." + Util.GetStringFromC_ASCII(tguiWidget_getWidgetType(ToolTipCPointer)));
				return (Widget)Activator.CreateInstance(type, new object[]{ ToolTipCPointer });
			}
			set
			{
				if (value != null)
					tguiWidget_setToolTip(CPointer, value.CPointer);
				else
					tguiWidget_setToolTip(CPointer, IntPtr.Zero);
			}
		}


		////////////////////////////////////////////////////////////
		/// <summary>
		/// Provide a string describing the object
		/// </summary>
		/// <returns>String description of the object</returns>
		////////////////////////////////////////////////////////////
		public override string ToString()
		{
			return "[Widget] Type(" + WidgetType + ")";
		}


		protected virtual void InitSignals()
		{
			IntPtr error;

		    PositionChangedCallback = new CallbackActionVector2f(ProcessPositionChangedSignal);
		    tguiWidget_connect_vector2f(CPointer, Util.ConvertStringForC_ASCII("PositionChanged"), PositionChangedCallback, out error);
		    if (error != IntPtr.Zero)
				throw new TGUIException(Util.GetStringFromC_ASCII(error));

		    SizeChangedCallback = new CallbackActionVector2f(ProcessSizeChangedSignal);
		    tguiWidget_connect_vector2f(CPointer, Util.ConvertStringForC_ASCII("SizeChanged"), SizeChangedCallback, out error);
		    if (error != IntPtr.Zero)
				throw new TGUIException(Util.GetStringFromC_ASCII(error));

		    MouseEnteredCallback = new CallbackAction(ProcessMouseEnteredSignal);
		    tguiWidget_connect(CPointer, Util.ConvertStringForC_ASCII("MouseEntered"), MouseEnteredCallback, out error);
		    if (error != IntPtr.Zero)
				throw new TGUIException(Util.GetStringFromC_ASCII(error));

		    MouseLeftCallback = new CallbackAction(ProcessMouseLeftSignal);
		    tguiWidget_connect(CPointer, Util.ConvertStringForC_ASCII("MouseLeft"), MouseLeftCallback, out error);
		    if (error != IntPtr.Zero)
				throw new TGUIException(Util.GetStringFromC_ASCII(error));
		}

		private void ProcessPositionChangedSignal(Vector2f pos)
		{
			if (PositionChanged != null)
				PositionChanged(this, new SignalArgsVector2f(pos));
		}

		private void ProcessSizeChangedSignal(Vector2f pos)
		{
			if (SizeChanged != null)
				SizeChanged(this, new SignalArgsVector2f(pos));
		}

		private void ProcessMouseEnteredSignal()
		{
			if (MouseEntered != null)
				MouseEntered(this, EventArgs.Empty);
		}

		private void ProcessMouseLeftSignal()
		{
			if (MouseLeft != null)
				MouseLeft(this, EventArgs.Empty);
		}

		/// <summary>Event handler for the Clicked signal</summary>
		public event EventHandler<SignalArgsVector2f> PositionChanged = null;

		/// <summary>Event handler for the Clicked signal</summary>
		public event EventHandler<SignalArgsVector2f> SizeChanged = null;

		/// <summary>Event handler for the MouseEntered signal</summary>
		public event EventHandler MouseEntered = null;

		/// <summary>Event handler for the MouseLeft signal</summary>
		public event EventHandler MouseLeft = null;

	    private CallbackActionVector2f PositionChangedCallback;
	    private CallbackActionVector2f SizeChangedCallback;
	    private CallbackAction         MouseEnteredCallback;
	    private CallbackAction         MouseLeftCallback;


	    protected Dictionary<string, List<uint>> myConnectedSignals = new Dictionary<string, List<uint>>();


		protected delegate void CallbackAction();
		protected delegate void CallbackActionVector2f(Vector2f param);
		protected delegate void CallbackActionString(IntPtr param);
		protected delegate void CallbackActionInt(int param);
		protected delegate void CallbackActionItemSelected(IntPtr param1, IntPtr param2);

		#region Imports

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected IntPtr tguiWidget_copy(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_destroy(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_setPosition(IntPtr cPointer, Vector2f pos);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_setPosition_fromLayout(IntPtr cPointer, IntPtr layout2d);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected Vector2f tguiWidget_getPosition(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected Vector2f tguiWidget_getAbsolutePosition(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected Vector2f tguiWidget_getWidgetOffset(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_setSize(IntPtr cPointer, Vector2f size);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_setSize_fromLayout(IntPtr cPointer, IntPtr layout2d);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected Vector2f tguiWidget_getSize(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected Vector2f tguiWidget_getFullSize(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected uint tguiWidget_connect(IntPtr cPointer, IntPtr signalName, [MarshalAs(UnmanagedType.FunctionPtr)] CallbackAction func, out IntPtr error);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_connect_vector2f(IntPtr cPointer, IntPtr signalName, [MarshalAs(UnmanagedType.FunctionPtr)] CallbackActionVector2f func, out IntPtr error);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_connect_string(IntPtr cPointer, IntPtr signalName, [MarshalAs(UnmanagedType.FunctionPtr)] CallbackActionString func, out IntPtr error);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_connect_int(IntPtr cPointer, IntPtr signalName, [MarshalAs(UnmanagedType.FunctionPtr)] CallbackActionInt func, out IntPtr error);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_connect_itemSelected(IntPtr cPointer, IntPtr signalName, [MarshalAs(UnmanagedType.FunctionPtr)] CallbackActionItemSelected func, out IntPtr error);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_disconnect(IntPtr cPointer, uint id);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_setRenderer(IntPtr cPointer, IntPtr rendererDataCPointer, out IntPtr error);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected IntPtr tguiWidget_getRenderer(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_show(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_showWithEffect(IntPtr cPointer, ShowAnimationType type, Time duration);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_hide(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_hideWithEffect(IntPtr cPointer, ShowAnimationType type, Time duration);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected bool tguiWidget_isVisible(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_enable(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_disable(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected bool tguiWidget_isEnabled(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected IntPtr tguiWidget_getWidgetType(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_moveToFront(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_moveToBack(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected void tguiWidget_setToolTip(IntPtr cPointer, IntPtr toolTipCPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected IntPtr tguiWidget_getToolTip(IntPtr cPointer);

		[DllImport("ctgui-0.8.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		static extern protected IntPtr tguiWidget_getParent(IntPtr cPointer);

		#endregion
	}
}