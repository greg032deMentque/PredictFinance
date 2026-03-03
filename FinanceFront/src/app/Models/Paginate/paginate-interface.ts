export class PaginateInterface<T> {
  constructor(
    public Datas: T[] = [],
    public Count: number = 0
  ) {}
}


/*
exemple : 

// dans votre service HTTP
getCompanies(): Observable<Paginate<Company>> {
  return this.http.get<Paginate<Company>>(url);
}

getUsers(): Observable<Paginate<User>> {
  return this.http.get<Paginate<User>>(url);
}

// en création manuelle
const page = new Paginate<Company>([c1, c2], 2);

*/