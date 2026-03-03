import { UserRoleEnum } from '../enums/UserRole.enum';
import { Company } from './company';
import { FixedPriceTimeSpent } from './fixed-price-time-spents';
import { UserDailyRate } from './user-daily-rate';
import { UserRole } from './user-role';


export class User {
  Id = "";
  UserName = "";
  Firstname = "";
  Lastname = "";
  Email = "";
  Created = new Date();
  LastActive = new Date();
  Password= "";
  PasswordPlanio = "";
  DailyRate = 0;
  Updated = new Date();
  FixedPriceTimeSpents: FixedPriceTimeSpent[] = [];
  UserDailyRates: UserDailyRate[] = [];
  Projects = [];
  Roles : UserRole[] = [];
  Companies : Company[] = [];
}
