export class AdminUser {
  id = '';
  firstName = '';
  lastName = '';
  email = '';
  role: 'User' | 'Admin' | 'SuperAdmin' = 'User';
  isActive = true;
  createdAt = '';
  lastLoginAt = '';

  constructor(init?: Partial<AdminUser>) {
    Object.assign(this, init);
  }
}

export class AdminDashboardStats {
  totalUsers = 0;
  activeUsers = 0;
  admins = 0;
  superAdmins = 0;
  usersCreatedLast7Days = 0;
  analysesToday = 0;
  queuedAnalyses = 0;
  failedAnalyses = 0;

  constructor(init?: Partial<AdminDashboardStats>) {
    Object.assign(this, init);
  }
}

export class AdminActivity {
  id = '';
  label = '';
  at = '';
  type: 'user' | 'analysis' | 'system' = 'user';

  constructor(init?: Partial<AdminActivity>) {
    Object.assign(this, init);
  }
}
