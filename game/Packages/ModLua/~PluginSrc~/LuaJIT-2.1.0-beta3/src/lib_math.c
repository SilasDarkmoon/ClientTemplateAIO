/*
** Math library.
** Copyright (C) 2005-2017 Mike Pall. See Copyright Notice in luajit.h
*/

#include <math.h>

#define lib_math_c
#define LUA_LIB

#include "lua.h"
#include "lauxlib.h"
#include "lualib.h"

#include "lj_obj.h"
#include "lj_lib.h"
#include "lj_vm.h"

/* ------------------------------------------------------------------------ */

#define LJLIB_MODULE_math

LJLIB_ASM(math_abs)		LJLIB_REC(.)
{
  lj_lib_checknumber(L, 1);
  return FFH_RETRY;
}
LJLIB_ASM_(math_floor)		LJLIB_REC(math_round IRFPM_FLOOR)
LJLIB_ASM_(math_ceil)		LJLIB_REC(math_round IRFPM_CEIL)

LJLIB_ASM(math_sqrt)		LJLIB_REC(math_unary IRFPM_SQRT)
{
  lj_lib_checknum(L, 1);
  return FFH_RETRY;
}
LJLIB_ASM_(math_log10)		LJLIB_REC(math_unary IRFPM_LOG10)
LJLIB_ASM_(math_exp)		LJLIB_REC(math_unary IRFPM_EXP)
LJLIB_ASM_(math_sin)		LJLIB_REC(math_unary IRFPM_SIN)
LJLIB_ASM_(math_cos)		LJLIB_REC(math_unary IRFPM_COS)
LJLIB_ASM_(math_tan)		LJLIB_REC(math_unary IRFPM_TAN)
LJLIB_ASM_(math_asin)		LJLIB_REC(math_atrig FF_math_asin)
LJLIB_ASM_(math_acos)		LJLIB_REC(math_atrig FF_math_acos)
LJLIB_ASM_(math_atan)		LJLIB_REC(math_atrig FF_math_atan)
LJLIB_ASM_(math_sinh)		LJLIB_REC(math_htrig IRCALL_sinh)
LJLIB_ASM_(math_cosh)		LJLIB_REC(math_htrig IRCALL_cosh)
LJLIB_ASM_(math_tanh)		LJLIB_REC(math_htrig IRCALL_tanh)
LJLIB_ASM_(math_frexp)
LJLIB_ASM_(math_modf)		LJLIB_REC(.)

LJLIB_ASM(math_log)		LJLIB_REC(math_log)
{
  double x = lj_lib_checknum(L, 1);
  if (L->base+1 < L->top) {
    double y = lj_lib_checknum(L, 2);
#ifdef LUAJIT_NO_LOG2
    x = log(x); y = 1.0 / log(y);
#else
    x = lj_vm_log2(x); y = 1.0 / lj_vm_log2(y);
#endif
    setnumV(L->base-1-LJ_FR2, x*y);  /* Do NOT join the expression to x / y. */
    return FFH_RES(1);
  }
  return FFH_RETRY;
}

LJLIB_LUA(math_deg) /* function(x) return x * 57.29577951308232 end */
LJLIB_LUA(math_rad) /* function(x) return x * 0.017453292519943295 end */

LJLIB_ASM(math_atan2)		LJLIB_REC(.)
{
  lj_lib_checknum(L, 1);
  lj_lib_checknum(L, 2);
  return FFH_RETRY;
}
LJLIB_ASM_(math_pow)		LJLIB_REC(.)
LJLIB_ASM_(math_fmod)

LJLIB_ASM(math_ldexp)		LJLIB_REC(.)
{
  lj_lib_checknum(L, 1);
#if LJ_DUALNUM && !LJ_TARGET_X86ORX64
  lj_lib_checkint(L, 2);
#else
  lj_lib_checknum(L, 2);
#endif
  return FFH_RETRY;
}

LJLIB_ASM(math_min)		LJLIB_REC(math_minmax IR_MIN)
{
  int i = 0;
  do { lj_lib_checknumber(L, ++i); } while (L->base+i < L->top);
  return FFH_RETRY;
}
LJLIB_ASM_(math_max)		LJLIB_REC(math_minmax IR_MAX)

LJLIB_PUSH(3.14159265358979323846) LJLIB_SET(pi)
LJLIB_PUSH(1e310) LJLIB_SET(huge)

/* ------------------------------------------------------------------------ */

/* This implements a Tausworthe PRNG with period 2^223. Based on:
**   Tables of maximally-equidistributed combined LFSR generators,
**   Pierre L'Ecuyer, 1991, table 3, 1st entry.
** Full-period ME-CF generator with L=64, J=4, k=223, N1=49.
*/

/* PRNG state. */
struct RandomState {
  uint64_t gen[4];	/* State of the 4 LFSR generators. */
  int valid;		/* State is valid. */
};

/* Union needed for bit-pattern conversion between uint64_t and double. */
typedef union { uint64_t u64; double d; } U64double;

/* Update generator i and compute a running xor of all states. */
#define TW223_GEN(i, k, q, s) \
  z = rs->gen[i]; \
  z = (((z<<q)^z) >> (k-s)) ^ ((z&((uint64_t)(int64_t)-1 << (64-k)))<<s); \
  r ^= z; rs->gen[i] = z;

/* PRNG step function. Returns a double in the range 1.0 <= d < 2.0. */
LJ_NOINLINE uint64_t LJ_FASTCALL lj_math_random_step(RandomState *rs)
{
  uint64_t z, r = 0;
  TW223_GEN(0, 63, 31, 18)
  TW223_GEN(1, 58, 19, 28)
  TW223_GEN(2, 55, 24,  7)
  TW223_GEN(3, 47, 21,  8)
  return (r & U64x(000fffff,ffffffff)) | U64x(3ff00000,00000000);
}

/* PRNG initialization function. */
static void random_init(RandomState *rs, double d)
{
  uint32_t r = 0x11090601;  /* 64-k[i] as four 8 bit constants. */
  int i;
  for (i = 0; i < 4; i++) {
    U64double u;
    uint32_t m = 1u << (r&255);
    r >>= 8;
    u.d = d = d * 3.14159265358979323846 + 2.7182818284590452354;
    if (u.u64 < m) u.u64 += m;  /* Ensure k[i] MSB of gen[i] are non-zero. */
    rs->gen[i] = u.u64;
  }
  rs->valid = 1;
  for (i = 0; i < 10; i++)
    lj_math_random_step(rs);
}

#ifndef LUA_USE_ORIGINAL_LJ_RANDOM
//-----change random generator------
#define N	16
#define MASK	((1 << (N - 1)) + (1 << (N - 1)) - 1)
#define LOW(x)	((unsigned)(x) & MASK)
#define HIGH(x)	LOW((x) >> N)
#define MUL(x, y, z)	{ int32_t l = (long)(x) * (long)(y); \
	(z)[0] = LOW(l); (z)[1] = HIGH(l); }
#define CARRY(x, y)	((int32_t)(x) + (long)(y) > MASK)
#define ADDEQU(x, y, z)	(z = CARRY(x, (y)), x = LOW(x + (y)))
#define X0	0x330E
#define X1	0xABCD
#define X2	0x1234
#define A0	0xE66D
#define A1	0xDEEC
#define A2	0x5
#define C	0xB
#define HI_BIT	(1L << (2 * N - 1))

typedef struct RandomState2 {
	uint32_t x0, x1, x2, a0, a1, a2, c;
	int valid;		/* State is valid. */
} RandomState2;

void next(RandomState2 *rs) {
	uint32_t p[2], q[2], r[2], carry0, carry1;

	MUL(rs->a0, rs->x0, p);
	ADDEQU(p[0], rs->c, carry0);
	ADDEQU(p[1], carry0, carry1);
	MUL(rs->a0, rs->x1, q);
	ADDEQU(p[1], q[0], carry0);
	MUL(rs->a1, rs->x0, r);
	rs->x2 = LOW(carry0 + carry1 + CARRY(p[1], r[0]) + q[1] + r[1] +
		rs->a0 * rs->x2 + rs->a1 * rs->x1 + rs->a2 * rs->x0);
	rs->x1 = LOW(p[1] + r[0]);
	rs->x0 = LOW(p[0]);
}

int32_t redisLrand48(RandomState2 *rs) {
	next(rs);
	return (((int32_t)rs->x2 << (N - 1)) + (rs->x1 >> 1));
}

static void redisSrand48(RandomState2 *rs, int32_t seedval) {
	rs->x0 = X0;
	rs->x1 = LOW(seedval);
	rs->x2 = HIGH(seedval);
	rs->a0 = A0;
	rs->a1 = A1;
	rs->a2 = A2;
	rs->c = C;
	rs->valid = 1;
}
#define EX_RAND_MAX 2147483647
#endif

/* PRNG extract function. */
LJLIB_PUSH(top-2)  /* Upvalue holds userdata with RandomState. */
LJLIB_CF(math_random)		LJLIB_REC(.)
{
  int n = (int)(L->top - L->base);
  RandomState *rs = (RandomState *)(uddata(udataV(lj_lib_upvalue(L, 1))));
  U64double u;
  double d;
  if (LJ_UNLIKELY(!rs->valid)) random_init(rs, 0.0);
  u.u64 = lj_math_random_step(rs);
  d = u.d - 1.0;
  if (n > 0) {
#if LJ_DUALNUM
    int isint = 1;
    double r1;
    lj_lib_checknumber(L, 1);
    if (tvisint(L->base)) {
      r1 = (lua_Number)intV(L->base);
    } else {
      isint = 0;
      r1 = numV(L->base);
    }
#else
    double r1 = lj_lib_checknum(L, 1);
#endif
    if (n == 1) {
      d = lj_vm_floor(d*r1) + 1.0;  /* d is an int in range [1, r1] */
    } else {
#if LJ_DUALNUM
      double r2;
      lj_lib_checknumber(L, 2);
      if (tvisint(L->base+1)) {
	r2 = (lua_Number)intV(L->base+1);
      } else {
	isint = 0;
	r2 = numV(L->base+1);
      }
#else
      double r2 = lj_lib_checknum(L, 2);
#endif
      d = lj_vm_floor(d*(r2-r1+1.0)) + r1;  /* d is an int in range [r1, r2] */
    }
#if LJ_DUALNUM
    if (isint) {
      setintV(L->top-1, lj_num2int(d));
      return 1;
    }
#endif
  }  /* else: d is a double in range [0, 1] */
  setnumV(L->top++, d);
  return 1;
}

LJLIB_PUSH(top-2)  /* Upvalue holds userdata with RandomState. */
LJLIB_CF(math_randomex)
{
#ifdef LUA_USE_ORIGINAL_LJ_RANDOM
  return 0;
#else
  int n = (int)(L->top - L->base);
  RandomState2 *rs = (RandomState2 *)(uddata(udataV(lj_lib_upvalue(L, 1))));
  if (LJ_UNLIKELY(!rs->valid)) redisSrand48(rs, 0);
  double d = (double)(redisLrand48(rs) % EX_RAND_MAX) / (double)EX_RAND_MAX;
  if (n > 0) {
#if LJ_DUALNUM
    int isint = 1;
    double r1;
    lj_lib_checknumber(L, 1);
    if (tvisint(L->base)) {
      r1 = (lua_Number)intV(L->base);
    } else {
      isint = 0;
      r1 = numV(L->base);
    }
#else
    double r1 = lj_lib_checknum(L, 1);
#endif
    if (n == 1) {
      d = lj_vm_floor(d*r1) + 1.0;  /* d is an int in range [1, r1] */
    } else {
#if LJ_DUALNUM
      double r2;
      lj_lib_checknumber(L, 2);
      if (tvisint(L->base+1)) {
	r2 = (lua_Number)intV(L->base+1);
      } else {
	isint = 0;
	r2 = numV(L->base+1);
      }
#else
      double r2 = lj_lib_checknum(L, 2);
#endif
      d = lj_vm_floor(d*(r2-r1+1.0)) + r1;  /* d is an int in range [r1, r2] */
    }
#if LJ_DUALNUM
    if (isint) {
      setintV(L->top-1, lj_num2int(d));
      return 1;
    }
#endif
  }  /* else: d is a double in range [0, 1] */
  setnumV(L->top++, d);
  return 1;
#endif
}


/* PRNG seed function. */
LJLIB_PUSH(top-2)  /* Upvalue holds userdata with RandomState. */
LJLIB_CF(math_randomseed)
{
#ifdef LUA_USE_ORIGINAL_LJ_RANDOM
  RandomState *rs = (RandomState *)(uddata(udataV(lj_lib_upvalue(L, 1))));
  random_init(rs, lj_lib_checknum(L, 1));
  return 0;
#else
  RandomState2 *rs = (RandomState2 *)(uddata(udataV(lj_lib_upvalue(L, 1))));
  redisSrand48(rs, (int32_t)lj_lib_checknum(L, 1));
  return 0;
#endif
}

/* ------------------------------------------------------------------------ */

#include "lj_libdef.h"

LUALIB_API int luaopen_math(lua_State *L)
{
#ifdef LUA_USE_ORIGINAL_LJ_RANDOM
  RandomState *rs;
  rs = (RandomState *)lua_newuserdata(L, sizeof(RandomState));
#else
  RandomState2 *rs;
  rs = (RandomState2 *)lua_newuserdata(L, sizeof(RandomState2));
#endif
  rs->valid = 0;  /* Use lazy initialization to save some time on startup. */
  LJ_LIB_REG(L, LUA_MATHLIBNAME, math);
#ifndef LUA_USE_ORIGINAL_LJ_RANDOM
  lua_getfield(L, -1, "randomex");
  lua_setfield(L, -2, "random");
#endif
  return 1;
}

