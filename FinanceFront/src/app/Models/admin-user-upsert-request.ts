export interface AdminUserUpsertRequest {
  userId?: string;
  firstName: string;
  lastName: string;
  email: string;
  password?: string;
  role: string;
  isActive: boolean;
  phoneNumber: string;
}
