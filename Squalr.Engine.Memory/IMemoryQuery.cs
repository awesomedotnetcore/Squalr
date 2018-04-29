﻿namespace Squalr.Engine.Memory
{
    using Squalr.Engine.OS;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An interface for querying virtual memory.
    /// </summary>
    public interface IMemoryQuery : IProcessObserver
    {
        /// <summary>
        /// Gets regions of memory allocated in the remote process based on provided parameters.
        /// </summary>
        /// <param name="requiredProtection">Protection flags required to be present.</param>
        /// <param name="excludedProtection">Protection flags that must not be present.</param>
        /// <param name="allowedTypes">Memory types that can be present.</param>
        /// <param name="startAddress">The start address of the query range.</param>
        /// <param name="endAddress">The end address of the query range.</param>
        /// <returns>A collection of pointers to virtual pages in the target process.</returns>
        IEnumerable<NormalizedRegion> GetVirtualPages(
            MemoryProtectionEnum requiredProtection,
            MemoryProtectionEnum excludedProtection,
            MemoryTypeEnum allowedTypes,
            UInt64 startAddress,
            UInt64 endAddress);

        /// <summary>
        /// Gets all virtual pages in the opened process.
        /// </summary>
        /// <returns>A collection of regions in the process.</returns>
        IEnumerable<NormalizedRegion> GetAllVirtualPages();

        /// <summary>
        /// Gets the maximum address possible in the target process.
        /// </summary>
        /// <returns>The maximum address possible in the target process.</returns>
        UInt64 GetMaximumAddress();

        /// <summary>
        /// Gets the maximum usermode address possible in the target process.
        /// </summary>
        /// <returns>The maximum usermode address possible in the target process.</returns>
        UInt64 GetMaxUsermodeAddress();

        /// <summary>
        /// Gets all modules in the opened process.
        /// </summary>
        /// <returns>A collection of modules in the process.</returns>
        IEnumerable<NormalizedModule> GetModules();

        /// <summary>
        /// Gets the address of the stacks in the opened process.
        /// </summary>
        /// <returns>A pointer to the stacks of the opened process.</returns>
        IEnumerable<NormalizedRegion> GetStackAddresses();

        /// <summary>
        /// Gets the addresses of the heaps in the opened process.
        /// </summary>
        /// <returns>A collection of pointers to all heaps in the opened process.</returns>
        IEnumerable<NormalizedRegion> GetHeapAddresses();
    }
    //// End interface
}
//// End namespace