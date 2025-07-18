/* tslint:disable */
/* eslint-disable */
/**
 * Data Analyst API
 * API for the Data Analyst project.
 *
 * The version of the OpenAPI document: v1
 * 
 *
 * NOTE: This class is auto generated by OpenAPI Generator (https://openapi-generator.tech).
 * https://openapi-generator.tech
 * Do not edit the class manually.
 */

import { mapValues } from '../runtime';
import type { User } from './User';
import {
    UserFromJSON,
    UserFromJSONTyped,
    UserToJSON,
} from './User';

/**
 * 
 * @export
 * @interface DataSession
 */
export interface DataSession {
    /**
     * 
     * @type {string}
     * @memberof DataSession
     */
    id?: string;
    /**
     * 
     * @type {string}
     * @memberof DataSession
     */
    name?: string | null;
    /**
     * 
     * @type {string}
     * @memberof DataSession
     */
    readonly schemaName?: string | null;
    /**
     * 
     * @type {Date}
     * @memberof DataSession
     */
    createdAt?: Date;
    /**
     * 
     * @type {Date}
     * @memberof DataSession
     */
    lastUpdatedAt?: Date;
    /**
     * 
     * @type {string}
     * @memberof DataSession
     */
    userId?: string | null;
    /**
     * 
     * @type {User}
     * @memberof DataSession
     */
    user?: User;
    /**
     * 
     * @type {boolean}
     * @memberof DataSession
     */
    initialFileHasHeaders?: boolean;
}

/**
 * Check if a given object implements the DataSession interface.
 */
export function instanceOfDataSession(value: object): value is DataSession {
    return true;
}

export function DataSessionFromJSON(json: any): DataSession {
    return DataSessionFromJSONTyped(json, false);
}

export function DataSessionFromJSONTyped(json: any, ignoreDiscriminator: boolean): DataSession {
    if (json == null) {
        return json;
    }
    return {
        
        'id': json['id'] == null ? undefined : json['id'],
        'name': json['name'] == null ? undefined : json['name'],
        'schemaName': json['schemaName'] == null ? undefined : json['schemaName'],
        'createdAt': json['createdAt'] == null ? undefined : (new Date(json['createdAt'])),
        'lastUpdatedAt': json['lastUpdatedAt'] == null ? undefined : (new Date(json['lastUpdatedAt'])),
        'userId': json['userId'] == null ? undefined : json['userId'],
        'user': json['user'] == null ? undefined : UserFromJSON(json['user']),
        'initialFileHasHeaders': json['initialFileHasHeaders'] == null ? undefined : json['initialFileHasHeaders'],
    };
}

export function DataSessionToJSON(value?: Omit<DataSession, 'schemaName'> | null): any {
    if (value == null) {
        return value;
    }
    return {
        
        'id': value['id'],
        'name': value['name'],
        'createdAt': value['createdAt'] == null ? undefined : ((value['createdAt']).toISOString()),
        'lastUpdatedAt': value['lastUpdatedAt'] == null ? undefined : ((value['lastUpdatedAt']).toISOString()),
        'userId': value['userId'],
        'user': UserToJSON(value['user']),
        'initialFileHasHeaders': value['initialFileHasHeaders'],
    };
}

