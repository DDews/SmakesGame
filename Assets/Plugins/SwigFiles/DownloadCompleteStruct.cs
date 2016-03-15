/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 2.0.2
 *
 * Do not make changes to this file unless you know what you are doing--modify
 * the SWIG interface file instead.
 * ----------------------------------------------------------------------------- */

namespace RakNet {

using System;
using System.Runtime.InteropServices;

public class DownloadCompleteStruct : IDisposable {
  private HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal DownloadCompleteStruct(IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new HandleRef(this, cPtr);
  }

  internal static HandleRef getCPtr(DownloadCompleteStruct obj) {
    return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
  }

  ~DownloadCompleteStruct() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          RakNetPINVOKE.delete_DownloadCompleteStruct(swigCPtr);
        }
        swigCPtr = new HandleRef(null, IntPtr.Zero);
      }
      GC.SuppressFinalize(this);
    }
  }

  public ushort setID {
    set {
      RakNetPINVOKE.DownloadCompleteStruct_setID_set(swigCPtr, value);
    } 
    get {
      ushort ret = RakNetPINVOKE.DownloadCompleteStruct_setID_get(swigCPtr);
      return ret;
    } 
  }

  public uint numberOfFilesInThisSet {
    set {
      RakNetPINVOKE.DownloadCompleteStruct_numberOfFilesInThisSet_set(swigCPtr, value);
    } 
    get {
      uint ret = RakNetPINVOKE.DownloadCompleteStruct_numberOfFilesInThisSet_get(swigCPtr);
      return ret;
    } 
  }

  public uint byteLengthOfThisSet {
    set {
      RakNetPINVOKE.DownloadCompleteStruct_byteLengthOfThisSet_set(swigCPtr, value);
    } 
    get {
      uint ret = RakNetPINVOKE.DownloadCompleteStruct_byteLengthOfThisSet_get(swigCPtr);
      return ret;
    } 
  }

  public SystemAddress senderSystemAddress {
    set {
      RakNetPINVOKE.DownloadCompleteStruct_senderSystemAddress_set(swigCPtr, SystemAddress.getCPtr(value));
    } 
    get {
      IntPtr cPtr = RakNetPINVOKE.DownloadCompleteStruct_senderSystemAddress_get(swigCPtr);
      SystemAddress ret = (cPtr == IntPtr.Zero) ? null : new SystemAddress(cPtr, false);
      return ret;
    } 
  }

  public RakNetGUID senderGuid {
    set {
      RakNetPINVOKE.DownloadCompleteStruct_senderGuid_set(swigCPtr, RakNetGUID.getCPtr(value));
    } 
    get {
      IntPtr cPtr = RakNetPINVOKE.DownloadCompleteStruct_senderGuid_get(swigCPtr);
      RakNetGUID ret = (cPtr == IntPtr.Zero) ? null : new RakNetGUID(cPtr, false);
      return ret;
    } 
  }

  public DownloadCompleteStruct() : this(RakNetPINVOKE.new_DownloadCompleteStruct(), true) {
  }

}

}
