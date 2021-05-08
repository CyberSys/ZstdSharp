﻿using System;

namespace ZstdSharp
{
    public unsafe class Compressor : IDisposable
    {
        public static int MinCompressionLevel => Methods.ZSTD_minCLevel();
        public static int MaxCompressionLevel => Methods.ZSTD_maxCLevel();
        public const int DefaultCompressionLevel = 0;

        private int level = DefaultCompressionLevel;

        private ZSTD_CCtx_s* cctx;

        public int Level
        {
            get => level;
            set
            {
                if (level != value)
                {
                    level = value;
                    SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, value);
                }
            }
        }

        public void SetParameter(ZSTD_cParameter parameter, int value)
        {
            EnsureNotDisposed();
            Methods.ZSTD_CCtx_setParameter(cctx, parameter, value).EnsureZstdSuccess();
        }

        public int GetParameter(ZSTD_cParameter parameter)
        {
            EnsureNotDisposed();
            int value;
            Methods.ZSTD_CCtx_getParameter(cctx, parameter, &value).EnsureZstdSuccess();
            return value;
        }

        public void LoadDictionary(byte[] dict)
        {
            EnsureNotDisposed();
            if (dict == null)
            {
                Methods.ZSTD_CCtx_loadDictionary(cctx, null, 0).EnsureZstdSuccess();
            }
            else
            {

                fixed (byte* dictPtr = dict)
                {
                    Methods.ZSTD_CCtx_loadDictionary(cctx, dictPtr, (nuint) dict.Length).EnsureZstdSuccess();
                }
            }
        }

        public Compressor(int level = DefaultCompressionLevel)
        {
            cctx = Methods.ZSTD_createCCtx();
            if (cctx == null)
            {
                throw new ZstdException(ZSTD_ErrorCode.ZSTD_error_GENERIC, "Failed to create cctx");
            }

            Level = level;
        }

        ~Compressor()
        {
            ReleaseUnmanagedResources();
        }

        public static int GetCompressBound(int length) 
            => (int) Methods.ZSTD_compressBound((nuint) length);

        public static ulong GetCompressBoundLong(ulong length)
            => Methods.ZSTD_compressBound((nuint) length);

        public Span<byte> Wrap(ReadOnlySpan<byte> src)
        {
            var dest = new byte[GetCompressBound(src.Length)];
            var length = Wrap(src, dest);
            return new Span<byte>(dest, 0, length);
        }

        public int Wrap(byte[] src, byte[] dest, int offset)
            => Wrap(src, new Span<byte>(dest, offset, dest.Length - offset));

        public int Wrap(ReadOnlySpan<byte> src, Span<byte> dest)
        {
            EnsureNotDisposed();
            fixed (byte* srcPtr = src)
            fixed (byte* destPtr = dest)
            {
                return (int) Methods.ZSTD_compress2(cctx, destPtr, (nuint) dest.Length, srcPtr, (nuint) src.Length)
                    .EnsureZstdSuccess();
            }
        }

        public int Wrap(ArraySegment<byte> src, ArraySegment<byte> dest) 
            => Wrap((ReadOnlySpan<byte>) src, dest);

        public int Wrap(byte[] src, int srcOffset, int srcLength, byte[] dst, int dstOffset, int dstLength) 
            => Wrap(new ReadOnlySpan<byte>(src, srcOffset, srcLength), new Span<byte>(dst, dstOffset, dstLength));

        private void ReleaseUnmanagedResources()
        {
            if (cctx != null)
            {
                Methods.ZSTD_freeCCtx(cctx);
                cctx = null;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (cctx == null)
            {
                throw new ObjectDisposedException(nameof(Compressor));
            }
        }
    }
}