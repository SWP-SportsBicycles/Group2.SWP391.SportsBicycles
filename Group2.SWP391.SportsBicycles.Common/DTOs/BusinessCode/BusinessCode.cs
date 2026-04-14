using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Common.DTOs.BusinessCode
{
    public enum BusinessCode
    {
        // ✅ SUCCESS (2xxx)
        GET_DATA_SUCCESSFULLY = 2000,
        SIGN_UP_SUCCESSFULLY = 2001,
        INSERT_SUCESSFULLY = 2002,
        UPDATE_SUCESSFULLY = 2003,
        DELETE_SUCESSFULLY = 2004,
        CREATED_SUCCESSFULLY = 2005,
        ALREADY_ACTIVE = 2006,

        // ⚠️ VALIDATION / CLIENT ERRORS (3xxx)
        VALIDATION_ERROR = 3000,
        VALIDATION_FAILED = 3001,
        INVALID_INPUT = 3002,
        INVALID_ACTION = 3003,
        INVALID_DATA = 3004,
        DUPLICATE_DATA = 3005,
        DATA_NOT_FOUND = 3006,
        EXISTED_USER = 3007,
        SIGN_UP_FAILED = 3008,
        WRONG_PASSWORD = 3009,
        AUTH_NOT_FOUND = 3010,
        ACCESS_DENIED = 3011,
        PERMISSION_DENIED = 3012,
        INSUFFICIENT_BALANCE = 3013,

        // 💥 SERVER / EXCEPTION (4xxx)
        INTERNAL_ERROR = 4000,
        EXCEPTION = 4001
    }

}
